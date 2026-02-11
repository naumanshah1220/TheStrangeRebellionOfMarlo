using UnityEngine;

namespace Detective.Gameplay
{
    [System.Serializable]
    public class CriminalViolation
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public float SeverityLevel { get; private set; }

        public CriminalViolation(string name, string description = "", float severityLevel = 1.0f)
        {
            Name = name;
            Description = description;
            SeverityLevel = severityLevel;
        }

        public override string ToString()
        {
            return Name;
        }
    }
} 