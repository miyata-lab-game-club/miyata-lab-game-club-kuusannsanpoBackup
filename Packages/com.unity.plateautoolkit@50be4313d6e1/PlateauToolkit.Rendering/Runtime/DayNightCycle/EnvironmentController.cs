using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif


namespace PlateauToolkit.Rendering
{
    [ExecuteInEditMode]
    public class EnvironmentController : MonoBehaviour
    {
        // make it singleton
        private static EnvironmentController m_instance;
        public static EnvironmentController Instance
        {
            get
            {
                return m_instance;
            }
        }

        public enum MaterialQuality
        {
            Low,
            Medium,
            High,
        }

        public MaterialQuality Quality = MaterialQuality.High;

        // Latitude, longitude
        public Vector2 Location = new Vector2(35.6895f, 139.69171f);
        public Vector3 Date = new Vector3(2023, 5, 10);
        public int TimeZone = 9;
        [Range(0, 1)]
        public float TimeOfDay;
        public float LerpSpeed = 1f;
        [Range(0, 1)]
        public float Cloud;
        public Color SunColor = Color.white;
        public Color MoonColor = new Color(0.0f, 0.2f, 0.3f);
        public float SunIntensity = 1;
        public float MoonIntensity = 1f;

        public float Rain = 0f;
        public float Snow = 0f;
        public float FogDistance = 500f;
        public float MaterialFade = 0f;

        private DateTime m_time;
        private float m_isNight;
        private VisualEffect m_rain;
        private VisualEffect m_snow;
        private Light m_sunLight;
        private Light m_moonLight;

#if UNITY_HDRP
        private HDAdditionalLightData m_sunHD;
        private HDAdditionalLightData m_moonHD;
#endif
        private DateTime m_dateTimeDelta;
        private Quaternion m_sunTargetRot;
        private Quaternion m_moonTargetRot;
        private GameObject m_globalVolume;
        private const double Deg2Rad = Math.PI / 180;
        private const double Rad2Deg = 180 / Math.PI;


        public void Awake()
        {
            if (m_instance != null)
            {
                m_instance = this;
            }

            m_rain = transform.Find("Rain")?.GetComponent<VisualEffect>();
            m_snow = transform.Find("Snow")?.GetComponent<VisualEffect>();

            m_sunLight = transform.Find("Sun")?.GetComponent<Light>();
            m_moonLight = transform.Find("Moon")?.GetComponent<Light>();

#if UNITY_HDRP
            if (m_sunLight != null)
            {
                m_sunHD = m_sunLight.GetComponent<HDAdditionalLightData>();
            }
            if (m_moonLight != null)
            {
                m_moonHD = m_moonLight.GetComponent<HDAdditionalLightData>();
            }

            // Set default value for HDRP
            if (SunIntensity == 1f)
                SunIntensity = 130000f;
            if (MoonIntensity == 1f)
                MoonIntensity = 100f;
#endif
        }

        public void SetMaterialQuality(MaterialQuality quality)
        {
#if UNITY_URP
            Quality = quality;
            switch (quality)
            {
                case MaterialQuality.Low:
                    Shader.EnableKeyword("MATERIAL_QUALITY_LOW");
                    Shader.DisableKeyword("MATERIAL_QUALITY_MEDIUM");
                    Shader.DisableKeyword("MATERIAL_QUALITY_HIGH");
                    break;
                case MaterialQuality.Medium:
                    Shader.DisableKeyword("MATERIAL_QUALITY_LOW");
                    Shader.EnableKeyword("MATERIAL_QUALITY_MEDIUM");
                    Shader.DisableKeyword("MATERIAL_QUALITY_HIGH");
                    break;
                case MaterialQuality.High:
                    Shader.DisableKeyword("MATERIAL_QUALITY_LOW");
                    Shader.DisableKeyword("MATERIAL_QUALITY_MEDIUM");
                    Shader.EnableKeyword("MATERIAL_QUALITY_HIGH");
                    break;
            }
#endif
        }

        private void Update()
        {
#if UNITY_HDRP
            if (!m_sunHD || !m_moonHD)
            {
                return;
            }
#endif
#if UNITY_URP
            if (!m_sunLight || !m_moonLight)
                return;
#endif
            UpdateLights();
            UpdateDayNightShift();

            Shader.SetGlobalFloat("_HideMaterial", MaterialFade);
        }

        private void UpdateLights()
        {
            m_time = CombineDateAndTime();
            m_time = m_time.ToUniversalTime();

            var sunDir = CalculateSunDirection(m_time, Location.x, Location.y);
            var moonDir = CalculateMoonDirection(m_time, Location.x, Location.y);
            m_sunTargetRot = Quaternion.Euler(sunDir);
            m_moonTargetRot = Quaternion.Euler(moonDir);

#if UNITY_HDRP
            m_sunHD.color = SunColor;
            m_moonHD.color = MoonColor;
#endif
#if UNITY_URP
            m_sunLight.color = CalculateSkyColor(sunDir.x) * SunColor;
            m_moonLight.color = MoonColor;
#endif

            Shader.SetGlobalFloat("_Cloud", Remap(Cloud, 0, 1, 0, 0.8f));
            Shader.SetGlobalFloat("_Rain", Rain);
            Shader.SetGlobalFloat("_Snow", Snow);
            // check if m_rain has particle rate
            if (m_rain)
            {
                if (m_rain.HasInt("Particle Rate"))
                {
                    m_rain.SetInt("Particle Rate", (int)(Rain * 20000));
                }
                if (m_rain.HasVector4("SunColor"))
                {
                    var particleBrightness = Color.white * (1 - m_isNight);
                    m_rain.SetVector4("SunColor", particleBrightness);
                }
                m_rain.transform.position = Camera.main.transform.position;
            }

            if (m_snow)
            {
                if (m_snow.HasInt("Particle Rate"))
                {
                    m_snow.SetInt("Particle Rate", (int)(Snow * 20000));
                    m_snow.transform.position = Camera.main.transform.position;
                }
                if (m_snow.HasVector4("SunColor"))
                {
                    var particleBrightness = Color.white * (1 - m_isNight);
                    m_snow.SetVector4("SunColor", particleBrightness);
                }
                m_snow.transform.position = Camera.main.transform.position;
            }

            m_dateTimeDelta = m_time;

            m_sunLight.transform.rotation = Quaternion.Lerp(m_sunLight.transform.rotation, m_sunTargetRot, LerpSpeed * Time.deltaTime);
            m_moonLight.transform.rotation = Quaternion.Lerp(m_moonLight.transform.rotation, m_moonTargetRot, LerpSpeed * Time.deltaTime);

            float threshold = 0.05f;
            float sunIntensity = 0f;
            float moonIntensity = 0f;

            if (m_sunLight.transform.forward.y < threshold && m_moonLight.transform.forward.y < threshold)
            {
#if UNITY_HDRP

                    sunIntensity = m_sunHD.intensity;
                    moonIntensity = m_moonHD.intensity;

                    float sunIntensitySmoothed = Mathf.SmoothStep(sunIntensity, SunIntensity, LerpSpeed * Time.deltaTime);
                    m_sunHD.intensity = sunIntensitySmoothed;

                    if (moonIntensity > 0.001f)
                    {
                        float moonIntensitySmoothed = Mathf.SmoothStep(moonIntensity, 0f, LerpSpeed * Time.deltaTime);
                        m_moonHD.intensity = moonIntensitySmoothed;
                    }
                    else
                    {
                        m_moonHD.intensity = 0f;
                    }
#endif

#if UNITY_URP
                    m_sunLight.intensity = Mathf.SmoothStep(m_sunLight.intensity, SunIntensity, LerpSpeed * Time.deltaTime);
                    if (m_moonLight.intensity > 0.001f)
                        m_moonLight.intensity = Mathf.SmoothStep(m_moonLight.intensity, 0f, LerpSpeed * Time.deltaTime);
                    else
                        m_moonLight.intensity = 0f;
                    RenderSettings.ambientSkyColor = m_sunLight.color;
#endif
                m_sunLight.shadows = LightShadows.Soft;
                m_moonLight.shadows = LightShadows.None;
                // Disable moonlight shadows
                Shader.SetGlobalFloat("_SkyMultiplier", m_sunLight.intensity);
            }
            else if (m_sunLight.transform.forward.y < threshold && m_moonLight.transform.forward.y > threshold)
            {
#if UNITY_HDRP
                    sunIntensity = m_sunHD.intensity;
                    moonIntensity = m_moonHD.intensity;

                    float sunIntensitySmoothed = Mathf.SmoothStep(sunIntensity, SunIntensity, LerpSpeed * Time.deltaTime);
                    m_sunHD.intensity = sunIntensitySmoothed;

                    if (moonIntensity > 0.001f)
                    {
                        float moonIntensitySmoothed = Mathf.SmoothStep(moonIntensity, 0f, LerpSpeed * Time.deltaTime);
                        m_moonHD.intensity = moonIntensitySmoothed;
                    }
                    else
                    {
                        m_moonHD.intensity = 0f;
                    }
#endif
#if UNITY_URP
                    m_sunLight.intensity = Mathf.SmoothStep(m_sunLight.intensity, SunIntensity, LerpSpeed * Time.deltaTime);
                    if (m_moonLight.intensity > 0.001f)
                        m_moonLight.intensity = Mathf.SmoothStep(m_moonLight.intensity, 0f, LerpSpeed * Time.deltaTime);
                    else
                        m_moonLight.intensity = 0f;
                    RenderSettings.ambientSkyColor = m_sunLight.color;
#endif
                m_sunLight.shadows = LightShadows.Soft;
                m_moonLight.shadows = LightShadows.None;
                Shader.SetGlobalFloat("_SkyMultiplier", m_sunLight.intensity);
            }
            else
            {
#if UNITY_HDRP
                    sunIntensity = m_sunHD.intensity;
                    moonIntensity = m_moonHD.intensity;

                    float moonIntensitySmoothed = Mathf.SmoothStep(moonIntensity, MoonIntensity, LerpSpeed * Time.deltaTime);
                    m_moonHD.intensity = moonIntensitySmoothed;

                    if (sunIntensity > 0.001f)
                    {
                        float sunIntensitySmoothed = Mathf.SmoothStep(sunIntensity, 0f, LerpSpeed * Time.deltaTime);
                        m_sunHD.intensity = sunIntensitySmoothed;
                    }
                    else
                    {
                        m_sunHD.intensity = 0f;
                    }
#endif
#if UNITY_URP
                    if (m_sunLight.intensity > 0.001f)
                        m_sunLight.intensity = Mathf.SmoothStep(m_sunLight.intensity, 0f, LerpSpeed * Time.deltaTime);
                    else
                        m_sunLight.intensity = 0f;
                    m_moonLight.intensity = Mathf.SmoothStep(m_moonLight.intensity, MoonIntensity, LerpSpeed * Time.deltaTime);
                    RenderSettings.ambientSkyColor = m_moonLight.color;
#endif
                m_sunLight.shadows = LightShadows.None;
                m_moonLight.shadows = LightShadows.Soft;
                Shader.SetGlobalFloat("_SkyMultiplier", m_moonLight.intensity);
            }

            // Use HDRP light activation
#if UNITY_HDRP
            m_sunHD.gameObject.SetActive(sunIntensity != 0);
            m_moonHD.gameObject.SetActive(moonIntensity != 0);
#endif
#if UNITY_URP
            m_sunLight.gameObject.SetActive(m_sunLight.intensity != 0);
            m_moonLight.gameObject.SetActive(m_moonLight.intensity != 0);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogEndDistance = FogDistance;
#endif
        }

        void SetProperty(object obj, string propertyName, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo != null)
            {
                MethodInfo overrideMethod = propertyInfo.PropertyType.GetMethod("Override", BindingFlags.Instance | BindingFlags.Public);
                if (overrideMethod != null)
                {
                    overrideMethod.Invoke(propertyInfo.GetValue(obj), new object[] { value });
                }
            }
        }

        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        void UpdateDayNightShift()
        {
            float y = m_sunLight.transform.forward.y;
            y = Mathf.Clamp(y, -0.2f, 0.2f);
            m_isNight = Remap(y, -0.2f, 0.2f, 0f, 1f);
            Shader.SetGlobalFloat("_IsNight", m_isNight);
        }

        Color CalculateSkyColor(float sunAltitude)
        {
            const float temperatureAtHorizon = 1000; // K
            const float temperatureAtZenith = 13000; // K

            var v = Remap(sunAltitude, -10, 90, 0, 1);
            float temperature = Mathf.Lerp(temperatureAtHorizon, temperatureAtZenith, v);
            temperature = Mathf.Clamp(temperature, 1000, 6500);
            temperature /= 100;

            float red, green, blue;
            if (temperature <= 66)
                red = 255;
            else
            {
                red = temperature - 60;
                red = 329.698727446f * Mathf.Pow(red, -0.1332047592f);
                red = Mathf.Clamp(red, 0, 255);
            }
            if (temperature <= 66)
            {
                green = temperature;
                green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;
                green = Mathf.Clamp(green, 0, 255);
            }
            else
            {
                green = temperature - 60;
                green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);
                green = Mathf.Clamp(green, 0, 255);
            }
            if (temperature >= 66)
                blue = 255;
            else
            {
                if (temperature <= 19)
                    blue = 0;
                else
                {
                    blue = temperature - 10;
                    blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
                    blue = Mathf.Clamp(blue, 0, 255);
                }
            }

            var col = new Color(red / 255f, green / 255f, blue / 255f);
            col = Color.Lerp(col, Color.white, Cloud);
            return col;
        }

        public string GetTimeString()
        {
            // Return 23:59 format from m_time which is DateTime
            var localTime = m_time.ToLocalTime();
            return localTime.ToString("HH:mm");

        }
        private DateTime CombineDateAndTime()
        {
            int year = (int)Date.x;
            int month = (int)Date.y;
            int day = (int)Date.z;
            int hour = (int)(TimeOfDay * 24);
            int minute = (int)((TimeOfDay * 24 - hour) * 60);
            if (TimeOfDay == 1)
                return new DateTime(year, month, day + 1, 0, 0, 0, TimeZone);

            return new DateTime(year, month, day, hour, minute, 0, TimeZone);
        }

        public Vector3 CalculateSunDirection(DateTime dateTime, float latitude, float longitude)
        {
            double julianDate = dateTime.ToOADate() + 2415018.5;

            double julianCenturies = julianDate / 36525.0;
            double siderealTimeHours = 6.6974 + 2400.0513 * julianCenturies;
            double siderealTimeUT = siderealTimeHours +
                (366.2422 / 365.2422) * dateTime.TimeOfDay.TotalHours;
            double siderealTime = siderealTimeUT * 15 + longitude;
            julianDate += dateTime.TimeOfDay.TotalHours / 24.0;
            julianCenturies = julianDate / 36525.0;
            double meanLongitude = NormalizeAngle(Deg2Rad * (280.466 + 36000.77 * julianCenturies));
            double meanAnomaly = NormalizeAngle(Deg2Rad * (357.529 + 35999.05 * julianCenturies));
            double equationOfCenter = Deg2Rad * ((1.915 - 0.005 * julianCenturies) *
                Math.Sin(meanAnomaly) + 0.02 * Math.Sin(2 * meanAnomaly));
            double elipticalLongitude = NormalizeAngle(meanLongitude + equationOfCenter);

            double obliquity = (23.439 - 0.013 * julianCenturies) * Math.PI / 180.0;
            double rightAscension = Math.Atan2(
                Math.Cos(obliquity) * Math.Sin(elipticalLongitude),
                Math.Cos(elipticalLongitude));
            double declination = Math.Asin(
                Math.Sin(rightAscension) * Math.Sin(obliquity));
            double hourAngle = NormalizeAngle(siderealTime * Math.PI / 180.0) - rightAscension;

            if (hourAngle > Math.PI)
                hourAngle -= 2 * Math.PI;

            double altitude = Math.Asin(Math.Sin(latitude * Math.PI / 180.0) *
                Math.Sin(declination) + Math.Cos(latitude * Math.PI / 180.0) *
                Math.Cos(declination) * Math.Cos(hourAngle));
            double aziNom = -Math.Sin(hourAngle);
            double aziDenom =
                Math.Tan(declination) * Math.Cos(latitude * Math.PI / 180.0) -
                Math.Sin(latitude * Math.PI / 180.0) * Math.Cos(hourAngle);

            double azimuth = Math.Atan(aziNom / aziDenom);

            if (aziDenom < 0)
                azimuth += Math.PI;
            else if (aziNom < 0)
                azimuth += 2 * Math.PI;
            altitude = altitude * Rad2Deg;
            azimuth = azimuth * Rad2Deg;

            return new Vector3((float)altitude, (float)azimuth, 0);

        }

        public Vector3 CalculateMoonDirection(DateTime dateTime, float latitude, float longitude)
        {
            double J1899 = 2415018.5;
            double J2000 = 2451545;
            double julianDate = (dateTime.ToOADate() + J1899) - J2000;

            var lw = Deg2Rad * -longitude;
            var phi = Deg2Rad * latitude;
            double eclipLongitude = (218.316 + 13.176396 * julianDate) * Deg2Rad;
            double lunarMeanAnomaly = (134.963 + 13.064993 * julianDate) * Deg2Rad;
            double lunarMeanDistance = (93.272 + 13.229350 * julianDate) * Deg2Rad;

            double lng = eclipLongitude + Deg2Rad * 6.289 * Math.Sin(lunarMeanAnomaly);
            double lat = Deg2Rad * 5.128 * Math.Sin(lunarMeanDistance);
            double distance = 385000 - 20905 * Math.Cos(lunarMeanAnomaly);

            double obliquity = Deg2Rad * 23.4397;
            double rightAscension = Math.Atan2(Math.Sin(lng) * Math.Cos(obliquity) - Math.Tan(lat) * Math.Sin(obliquity), Math.Cos(lng));
            double dec = Math.Asin(Math.Sin(lng) * Math.Cos(obliquity) + Math.Cos(lat) * Math.Sin(obliquity) * Math.Sin(longitude));

            double h = (Deg2Rad * (280.16 + 360.9856235 * julianDate) - lw) - rightAscension;
            double altitude = Math.Asin(Math.Sin(phi) * Math.Sin(dec) + Math.Cos(phi) * Math.Cos(dec) * Math.Cos(h));
            double azimuth = Math.Atan2(Math.Sin(h), Math.Cos(h) * Math.Sin(phi) - Math.Tan(dec) * Math.Cos(phi));

            altitude = altitude * Rad2Deg;
            azimuth = azimuth * Rad2Deg;

            return new Vector3((float)altitude, (float)azimuth, 0);
        }

        private double NormalizeAngle(double inputAngle)
        {
            double twoPi = 2 * Mathf.PI;

            if (inputAngle < 0)
            {
                return twoPi - (Math.Abs(inputAngle) % twoPi);
            }
            else if (inputAngle > twoPi)
            {
                return inputAngle % twoPi;
            }
            else
            {
                return inputAngle;
            }
        }

    }
}