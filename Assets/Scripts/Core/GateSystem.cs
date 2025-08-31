using UnityEngine;

public class GateSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem gateParticle;
    [SerializeField] private GameObject knifePrefab; // Animator’dan deðil, prefab’tan instantiate edeceksen

    public BlockController blockController;

    private Animator gateAnimator;

    private void Start()
    {
        gateAnimator = GetComponent<Animator>();
        knifePrefab = transform.Find("KnifePrefab").gameObject; // KnifePrefab objesini bul

    }

    private void LateUpdate()
    {
        if (transform.localScale.y > 1f)
        {
            knifePrefab.transform.localScale = new Vector3(315.61377f, 71.2227097f, 41.7028885f);
        }

        // Eðer gate’in scale.x veya scale.y > 1 ise sabit boyut ayarla
        if (transform.localScale.x > 1f)
        {
            knifePrefab.transform.localScale = new Vector3(315.61377f, 47.3681602f, 72.8639832f);
        }

    }

    private void OnEnable()
    {
        BlockController.GateAction += ActivateGate;
    }

    private void OnDisable()
    {
        BlockController.GateAction -= ActivateGate;
    }

    private void ActivateGate(BlockController acBlock, GateSystem gateSystem)
    {
        // Sadece bana geldiyse çalýþ
        if (gateSystem != this) return;

        if (transform.localScale.x == 0f)
        {
            gateAnimator.SetTrigger("ReverseKnifeAnim");
        } else
        {
            gateAnimator.SetTrigger("KnifeAnim");
        }

        var color = acBlock.GetComponent<Renderer>().material.color;
        if (color.a < 0.99f) color.a = 1f;

        var main = gateParticle.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color);

        gateParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gateParticle.Clear(true);
        gateParticle.Simulate(0f, true, true, true);
        gateParticle.Play(true);
    }
}
