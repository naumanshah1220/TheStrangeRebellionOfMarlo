using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class ReportAssetCreator : MonoBehaviour
{
    [ContextMenu("Create Sample Report")]
    public void CreateSampleReport()
    {
        Report report = ScriptableObject.CreateInstance<Report>();
        report.id = "report_001";
        report.title = "Forensic Analysis Report";
        report.description = "Detailed forensic analysis of evidence collected from the crime scene, including fingerprint analysis, DNA testing, and ballistic examination.";
        report.author = "Detective Michael Chen";
        report.department = "Forensic Science Division";
        report.reportDate = "2024-01-15";
        report.caseNumber = "CASE-2024-001";
        report.reportType = ReportType.Forensic;
        
        AssetDatabase.CreateAsset(report, "Assets/Resources/Reports/SampleReport.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = report;
        
        Debug.Log("Sample Report asset created!");
    }
}
#endif
