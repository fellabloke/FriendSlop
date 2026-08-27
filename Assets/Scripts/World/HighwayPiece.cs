using UnityEngine;

[CreateAssetMenu(fileName = "HighwayPiece", menuName = "Scriptable Objects/HighwayPiece")]
public class HighwayPiece : ScriptableObject
{
    public enum RoadType
    {
        Straight,
        CurveLeft,
        CurveRight,
        Obstacle,
        Special 
    }

    [System.Serializable]
    public struct RoadSegmentData
    {
        public string segmentName;
        public GameObject prefab;
        public RoadType type;
        [Range(0f, 100f)]
        public float weight;
        public float angleChange;
    }
}
