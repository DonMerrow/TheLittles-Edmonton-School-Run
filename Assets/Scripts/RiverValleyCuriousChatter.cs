using UnityEngine;

/// <summary>A small self-appointed reporter keeps Danny informed about the group.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyCuriousChatter : MonoBehaviour
{
    private RiverValleyGameDirector director;
    private Transform danny;
    private float nextReport;
    private string lastReportKind;

    public static int QuestionsVoiced { get; private set; }
    public static int ReportsVoiced { get; private set; }

    private void Start()
    {
        director = FindFirstObjectByType<RiverValleyGameDirector>();
        DannySpark found=FindFirstObjectByType<DannySpark>();
        danny=found!=null?found.transform:null;
        nextReport = Time.time + 20f;
    }

    private void Update()
    {
        // The reporter is flavour, not a navigation alarm. Keep the school
        // entrance and ending quiet so an optional reminder can never talk
        // over the principal or make the finale feel stuck.
        if (director == null || danny==null || director.IsBusy ||
            director.LevelTwoChoiceReady || director.Progress >= 0.94f ||
            director.RescuedKids <= 0 || Time.time < nextReport) return;
        RiverValleyKidFollower[] followers=FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None);
        int buried=0;
        int scattered=0;
        float furthest=0f;
        foreach(RiverValleyKidFollower follower in followers)
        {
            if(follower==null)continue;
            if(follower.IsBuried)buried++;
            else if(follower.IsScattered)scattered++;
            else if(follower.IsWaiting)continue;
            else furthest=Mathf.Max(furthest,Vector3.ProjectOnPlane(follower.transform.position-danny.position,Vector3.up).magnitude);
        }

        string report=null;
        string reportKind=null;
        if(buried>0)
        {
            reportKind="buried";
            report="Danny, somebody is stuck in the snow.";
        }
        else if(scattered>0)
        {
            reportKind="scattered";
            report=$"Danny, {scattered} kids left the group. I am reporting this because I am responsible.";
        }
        else if(furthest>10f)
        {
            reportKind="behind";
            report="Danny, some little kids need a moment to catch up.";
        }
        if(report!=null)
        {
            // Say each situation once. It becomes eligible again only after
            // the group recovers, rather than repeating every few seconds.
            if(reportKind==lastReportKind)
            {
                nextReport=Time.time+8f;
                return;
            }
            director.Show("LITTLE REPORTER",report,4.4f);
            QuestionsVoiced++;
            ReportsVoiced++;
            lastReportKind=reportKind;
            nextReport=Time.time+Random.Range(28f,38f);
        }
        else
        {
            lastReportKind=null;
            nextReport=Time.time+6f;
        }
    }
}
