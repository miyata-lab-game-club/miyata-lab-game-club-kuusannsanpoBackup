using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PlateauToolkit.Sandbox
{
    /// <summary>
    /// Defines an entity running on tracks.
    /// </summary>
    public interface IPlateauSandboxTrackRunner
    {
        /// <summary>
        /// Move delta how much the object moves on a frame.
        /// </summary>
        /// <returns></returns>
        float GetMoveDelta();

        /// <summary>
        /// Distance detecting collision.
        /// </summary>
        /// <returns></returns>
        float GetCollisionDistance();
    }

    /// <summary>
    /// Represents a track where objects can move, controls <see cref="Spline" />
    /// and has additional data for specific features of Sandbox Toolkit.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class PlateauSandboxTrack : MonoBehaviour
    {
        static readonly List<SplineKnotIndex> k_CalcKnotsList = new(128);

        [SerializeField] float m_SpeedLimit;
        [SerializeField] bool m_HasSpeedLimit;

        /// <summary>
        /// The reference of attached <see cref="UnityEngine.Splines.SplineContainer" />
        /// </summary>
        /// <remarks>
        /// Do not use this field, use <see cref="SplineContainer"/>
        /// </remarks>
        SplineContainer m_SplineContainerReference;

        /// <summary>
        /// The <see cref="UnityEngine.Splines.SplineContainer" /> which contains a <see cref="Spline" />.
        /// </summary>
        internal SplineContainer SplineContainer
        {
            get
            {
                if (m_SplineContainerReference == null)
                {
                    m_SplineContainerReference = GetComponent<SplineContainer>();
                }
                return m_SplineContainerReference;
            }
        }

        /// <summary>
        /// The speed limit on the track.
        /// </summary>
        public float? SpeedLimit => m_HasSpeedLimit ? m_SpeedLimit : null;

        /// <summary>
        /// The max interpolation value of the <see cref="SplineContainer" />
        /// </summary>
        public float MaxSplineContainerT => SplineContainer.Splines.Count;

        public int GetTrackId()
        {
            return gameObject.GetHashCode();
        }

        /// <summary>
        /// Enumerate interpolation values of positions in the <see cref="SplineContainer" /> to random walk.
        /// </summary>
        /// <returns>an interpolation value in the spline container, the move delta and velocity</returns>
        public IEnumerator<(float, float)> GetRandomWalkWithCollision(
            float startSplineContainerT, int randomPathSeed, IPlateauSandboxTrackRunner trackRunner)
        {
            if (SplineContainer.Splines.Count == 0)
            {
                yield break;
            }
            if (SplineContainer.Splines[0].Count == 0)
            {
                yield break;
            }

            // Calculate a start point from given startSplineContainerT and create a path enumerator.
            IEnumerator<TrackPath> pathEnumerator;
            float curveT;
            {
                SplineKnotIndex startKnotIndex;
                bool startKnotRandom;
                {
                    (int splineIndex, float splineT) = GetSplineIndex(startSplineContainerT);
                    startKnotRandom = splineT == 0f;

                    Spline spline = SplineContainer.Splines[splineIndex];
                    int knotIndex = spline.SplineToCurveT(splineT, out curveT);

                    startKnotIndex = new SplineKnotIndex(splineIndex, knotIndex);
                }

                pathEnumerator = GetRandomWalkPaths(startKnotIndex, startKnotRandom, randomPathSeed);
            }

            // Prepare TrackPathIterator.
            LinkedList<TrackPath> sharedPaths = new();
            TrackPathIterator pathIterator = new(pathEnumerator, sharedPaths, true);

            // Clone the path iterator for collision detection and using exactly the same paths of the main paths.
            TrackPathIterator collisionPathIterator = pathIterator.Clone();

            if (!pathIterator.MoveNextPath())
            {
                // Just in case track doesn't have enough paths.
                yield break;
            }

            // Enumerate the interpolation positions.
            while (true)
            {
                float moveDelta = trackRunner.GetMoveDelta();

                // Get an interpolation value where the current position moved to.
                if (!FindMovedPoint(pathIterator, moveDelta, ref curveT, out float t))
                {
                    yield return (t, t);
                    yield break;
                }

                // Get an iterpolation value of the collision detection point.
                collisionPathIterator.CopyState(pathIterator);
                FindMovedPoint(collisionPathIterator, curveT, trackRunner.GetCollisionDistance(), out float collisionT);

                yield return (t, collisionT);
            }
        }

        void FindMovedPoint(TrackPathIterator pathIterator, float startCurveT, float delta, out float t)
        {
            FindMovedPoint(pathIterator, delta, ref startCurveT, out t);
        }

        bool FindMovedPoint(TrackPathIterator pathIterator, float delta, ref float curveT, out float t)
        {
            // Find the position that is moved by the given delta from the starting point.
            // (NOTE) Depending on the value of the delta, need to iterate pathIterator.
            while (true)
            {
                float previousStartCurveT = curveT;
                curveT += delta / pathIterator.CurrentPath.CurveLength;
                if (curveT < 1f)
                {
                    break;
                }

                // In the case that the requested position exceeds the current path.
                delta -= (1 - previousStartCurveT) * pathIterator.CurrentPath.CurveLength;

                if (!pathIterator.MoveNextPath())
                {
                    t = pathIterator.CurrentPath.GetSplineContainerT(1);
                    return false;
                }
                curveT = 0;
            }

            t = pathIterator.CurrentPath.GetSplineContainerT(curveT);
            return true;
        }

        /// <summary>
        /// Enumerates randomly selected paths on the track.
        /// </summary>
        /// <remarks>
        /// This collection will be an infinite list if the spline has a loop.
        /// </remarks>
        /// <returns></returns>
        IEnumerator<TrackPath> GetRandomWalkPaths(
            SplineKnotIndex startKnotIndex, bool startKnotRandom, int seed)
        {
            var random = new System.Random(seed);

            if (SplineContainer.Splines.Count == 0)
            {
                yield break;
            }
            if (SplineContainer.Splines[0].Count == 0)
            {
                yield break;
            }

            if (startKnotRandom)
            {
                RandomSelectIfLinked(startKnotIndex, out startKnotIndex);
            }
            SplineKnotIndex currentKnotIndex = startKnotIndex;

            while (true)
            {
                Spline currentSpline = SplineContainer.Splines[currentKnotIndex.Spline];
                float length = CurveUtility.CalculateLength(
                    currentSpline.GetCurve(currentKnotIndex.Knot).Transform(transform.localToWorldMatrix));

                yield return new TrackPath
                {
                    Spline = currentSpline,
                    SplineIndex = currentKnotIndex.Spline,
                    KnotIndex = currentKnotIndex.Knot,
                    CurveLength = length,
                };

                // The next knot in the same spline
                SplineKnotIndex incrementedKnotIndex = new(currentKnotIndex.Spline, currentKnotIndex.Knot + 1);

                if (incrementedKnotIndex.Knot < (currentSpline.Closed ? currentSpline.Count : currentSpline.Count - 1))
                {
                    RandomSelectIfLinked(incrementedKnotIndex, out currentKnotIndex);
                }
                else // The knot is the last one in its spline.
                {
                    if (currentSpline.Closed)
                    {
                        // If the spline is closed, the spline loops.
                        RandomSelectIfLinked(new(currentKnotIndex.Spline, 0), out currentKnotIndex);
                    }
                    else
                    {
                        // If the spline is opened but the last knot is linked, the spline loops.
                        if (!RandomSelectIfLinked(incrementedKnotIndex, out currentKnotIndex))
                        {
                            // The next knot doesn't have any linked knot, then it's terminal.
                            // Random walk paths can last anymore, finish the enumeration.
                            yield break;
                        }
                    }
                }
            }

            // Select a knot at random in the given knot and the other knots linked to it.
            // If the knot isn't linked, the knot will be returned.
            bool RandomSelectIfLinked(SplineKnotIndex splineKnotIndex, out SplineKnotIndex selectedKnotIndex)
            {
                IReadOnlyList<SplineKnotIndex> linkedKnots;
                if (!SplineContainer.KnotLinkCollection.TryGetKnotLinks(splineKnotIndex, out linkedKnots))
                {
                    // The knot isn't linked, the return the knot itself.
                    selectedKnotIndex = splineKnotIndex;
                    return false;
                }

                foreach (SplineKnotIndex linkedKnot in linkedKnots)
                {
                    Spline spline = SplineContainer.Splines[linkedKnot.Spline];
                    if (!spline.Closed && linkedKnot.Knot == spline.Count - 1)
                    {
                        // Filter the terminal knots if the spline is opened.
                        continue;
                    }

                    k_CalcKnotsList.Add(linkedKnot);
                }

                // Select a knot at random in the available knots.
                selectedKnotIndex = k_CalcKnotsList[random.Next(0, k_CalcKnotsList.Count)];
                k_CalcKnotsList.Clear();

                return true;
            }
        }

        /// <summary>
        /// Get the start point of the <see cref="SplineContainer" />
        /// </summary>
        /// <returns></returns>
        public Vector3 GetStartPosition()
        {
            foreach (Spline spline in GetComponent<SplineContainer>().Splines)
            {
                foreach (BezierKnot knot in spline.Knots)
                {
                    return knot.Position;
                }
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Get the count of knots in the spline.
        /// </summary>
        /// <returns></returns>
        public int GetKnotsCount()
        {
            int count = 0;
            foreach (Spline spline in SplineContainer.Splines)
            {
                count += spline.Knots.Count();
            }

            return count;
        }

        /// <summary>
        /// Get all curves in the spline.
        /// </summary>
        /// <param name="curves">the result of curves</param>
        public void GetCurves(List<BezierCurve> curves)
        {
            foreach (Spline spline in SplineContainer.Splines)
            {
                int curveCount = spline.Closed ? spline.Count : spline.Count - 1;
                for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
                {
                    BezierCurve curve = spline.GetCurve(curveIndex).Transform(transform.localToWorldMatrix);
                    curves.Add(curve);
                }
            }
        }

        public float GetNearestPoint(Vector3 position, out Vector3 nearestPoint, out int nearestSplineIndex, out float nearestT)
        {
            nearestPoint = Vector3.zero;
            nearestSplineIndex = 0;
            nearestT = 0;

            float nearestDistance = float.PositiveInfinity;
            for (int splineIndex = 0; splineIndex < SplineContainer.Splines.Count; splineIndex++)
            {
                Spline spline = SplineContainer[splineIndex];
                float distance = SplineUtility.GetNearestPoint(
                    spline, position, out float3 p, out float t,
                    SplineUtility.PickResolutionMax, SplineUtility.PickResolutionMax);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSplineIndex = splineIndex;
                    nearestPoint = p;
                    nearestT = t;
                }
            }

            return nearestDistance;
        }

        /// <summary>
        /// Get the transform info of a point on the splines by the spline interpolation.
        /// </summary>
        /// <param name="splineContainerT">
        /// an interpolation value in the <see cref="SplineContainer" />.
        /// </param>
        public (Vector3 position, Vector3 forward, Vector3 up) GetTransformBySplineContainerT(float splineContainerT)
        {
            (int splineIndex, float t) = GetSplineIndex(splineContainerT);

            Spline spline = SplineContainer[splineIndex];
            return (
                transform.TransformPoint(spline.EvaluatePosition(t)),
                transform.TransformDirection(spline.EvaluateTangent(t)).normalized,
                transform.TransformDirection(spline.EvaluateUpVector(t)).normalized);
        }

        public (Vector3, Vector3) GetPositionAndUpBySplineContainerT(float splineContainerT)
        {
            (int splineIndex, float t) = GetSplineIndex(splineContainerT);

            Spline spline = SplineContainer[splineIndex];
            return (
                transform.TransformPoint(spline.EvaluatePosition(t)),
                transform.TransformDirection(spline.EvaluateUpVector(t)));
        }

        /// <summary>
        /// Get the index and the interpolation value from the interpolation value
        /// normalized in the <see cref="SplineContainer" />
        /// </summary>
        /// <remarks>
        /// The start of a knot and the end of the next knot are same.
        /// To handle the case where the knots in different splines are linked,
        /// regard it as the end of the next when splineT is an integer.
        /// </remarks>
        /// <param name="splineContainerT"></param>
        /// <returns></returns>
        (int splineIndex, float t) GetSplineIndex(float splineContainerT)
        {
            int splineIndex = (int)math.floor(splineContainerT);
            float t = math.frac(splineContainerT);
            if (splineIndex > 0 && t == 0f)
            {
                return (splineIndex - 1, 1f);
            }
            else
            {
                return (splineIndex, t);
            }
        }

        class TrackPathIterator
        {
            readonly LinkedList<TrackPath> m_Paths;
            readonly bool m_RemoveHistory;
            readonly IEnumerator<TrackPath> m_PathEnumerator;

            LinkedListNode<TrackPath> m_CurrentPathNode;

            public TrackPathIterator(IEnumerator<TrackPath> pathEnumerator, LinkedList<TrackPath> paths, bool removeHistory)
            {
                m_PathEnumerator = pathEnumerator;
                m_Paths = paths;
                m_RemoveHistory = removeHistory;
                m_CurrentPathNode = null;
            }

            public TrackPath CurrentPath => m_CurrentPathNode.Value;
            public bool HasCurrentPath => m_CurrentPathNode != null;

            LinkedListNode<TrackPath> MoveNextPathInternal()
            {
                if (!m_PathEnumerator.MoveNext())
                {
                    return null;
                }

                return m_Paths.AddLast(m_PathEnumerator.Current);
            }

            public bool MoveNextPath()
            {
                LinkedListNode<TrackPath> nextPathNode;
                if (m_CurrentPathNode == null)
                {
                    if (m_Paths.Count == 0)
                    {
                        nextPathNode = MoveNextPathInternal();
                    }
                    else
                    {
                        nextPathNode = m_Paths.First;
                    }
                }
                else if (m_CurrentPathNode.Next == null)
                {
                    nextPathNode = MoveNextPathInternal();
                }
                else
                {
                    nextPathNode = m_CurrentPathNode.Next;
                }

                if (nextPathNode == null)
                {
                    return false;
                }

                if (m_RemoveHistory && m_CurrentPathNode != null)
                {
                    m_Paths.Remove(m_CurrentPathNode);
                }

                m_CurrentPathNode = nextPathNode;
                return true;
            }

            public void CopyState(TrackPathIterator other)
            {
                Debug.Assert(m_Paths == other.m_Paths);
                Debug.Assert(m_PathEnumerator == other.m_PathEnumerator);
                m_CurrentPathNode = other.m_CurrentPathNode;
            }

            public TrackPathIterator Clone()
            {
                return new TrackPathIterator(m_PathEnumerator, m_Paths, false);
            }
        }

        struct TrackPath
        {
            public Spline Spline { get; set; }
            public int SplineIndex { get; set; }
            public int KnotIndex { get; set; }
            public float CurveLength { get; set; }

            public float GetSplineContainerT(float curveT)
            {
                return SplineIndex + Spline.CurveToSplineT(KnotIndex + curveT);
            }
        }
    }
}