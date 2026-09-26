using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Opt-in runtime check that the licensed rabbits are grounded and visibly animated.</summary>
public sealed class RiverValleyRabbitAuditDriver : MonoBehaviour
{
    private readonly RaycastHit[] hits=new RaycastHit[32];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverRabbitAudit"))return;
        GameObject runner=new("RABBIT AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyRabbitAuditDriver>();
    }

    private IEnumerator Start()
    {
        Application.runInBackground=true;
        QualitySettings.vSyncCount=0;
        Time.timeScale=2f;
        yield return new WaitForSeconds(0.7f);
        List<RiverValleyAnimalMotion> rabbits=FindObjectsByType<RiverValleyAnimalMotion>(FindObjectsSortMode.None)
            .Where(item=>item!=null&&item.Kind==RiverAnimalKind.Rabbit&&item.gameObject.activeInHierarchy).ToList();
        List<string> report=new(){$"Active animated rabbits: {rabbits.Count}."};
        bool grounded=true;
        foreach(RiverValleyAnimalMotion rabbit in rabbits)
        {
            Renderer[] visuals=rabbit.GetComponentsInChildren<Renderer>(true);
            if(visuals.Length==0){grounded=false;report.Add(rabbit.name+": no visible model.");continue;}
            Bounds bounds=visuals[0].bounds;
            for(int i=1;i<visuals.Length;i++)bounds.Encapsulate(visuals[i].bounds);
            float ground=FindGroundBelow(rabbit.transform,bounds.center);
            float pawGap=bounds.min.y-ground;
            report.Add($"{rabbit.name}: paw gap {pawGap:F3} m; animator {rabbit.GetComponentInChildren<QuaterniusAnimalAnimator>(true)!=null}.");
            if(float.IsNegativeInfinity(ground)||pawGap < -0.07f||pawGap>0.09f||
               rabbit.GetComponentInChildren<QuaterniusAnimalAnimator>(true)==null)grounded=false;
        }
        int poseBefore=QuaterniusAnimalAnimator.PoseVerifiedCount;
        int jumpsBefore=QuaterniusAnimalAnimator.RabbitJumpsStarted;
        int visibleJumpsBefore=RiverValleyAnimalMotion.VisibleRabbitJumps;
        yield return new WaitForSeconds(9f);
        report.Add($"Verified animated poses: {QuaterniusAnimalAnimator.PoseVerifiedCount}; Blender jump clips: {QuaterniusAnimalAnimator.RabbitJumpsStarted-jumpsBefore}; visible grounded hops: {RiverValleyAnimalMotion.VisibleRabbitJumps-visibleJumpsBefore}.");
        bool passed=rabbits.Count>=6&&grounded&&QuaterniusAnimalAnimator.PoseVerifiedCount>=poseBefore&&
            QuaterniusAnimalAnimator.RabbitJumpsStarted>jumpsBefore&&
            RiverValleyAnimalMotion.VisibleRabbitJumps>visibleJumpsBefore;
        report.Add(passed?"PASS: rabbits are grounded and their idle/jump animation is running.":
            "FAIL: rabbits were missing, floating, or did not animate and jump.");
        Directory.CreateDirectory("/tmp/thelittles-rabbit-audit");
        File.WriteAllLines("/tmp/thelittles-rabbit-audit/report.txt",report);
        Application.Quit(passed?0:2);
    }

    private float FindGroundBelow(Transform animal,Vector3 centre)
    {
        int count=Physics.RaycastNonAlloc(centre+Vector3.up*5f,Vector3.down,hits,20f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float highest=float.NegativeInfinity;
        for(int i=0;i<count;i++)
            if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(animal)&&
               hits[i].point.y<=centre.y+0.2f&&IsGround(hits[i].collider,hits[i].normal))
                highest=Mathf.Max(highest,hits[i].point.y);
        return highest;
    }

    private static bool IsGround(Collider collider,Vector3 normal)
    {
        if(collider==null||normal.y<0.48f)return false;
        string label=(collider.name+" "+collider.transform.root.name).ToLowerInvariant();
        return !label.Contains("building")&&!label.Contains("roof")&&!label.Contains("solid exterior")&&
            !label.Contains("safety boundary")&&!label.Contains("sign")&&!label.Contains("tree")&&
            !label.Contains("trunk")&&!label.Contains("branch")&&!label.Contains("vehicle")&&
            !label.Contains("plow")&&!label.Contains("firetruck");
    }
}
