using UnityEngine;

public class SnowSystem : MonoBehaviour
{
    [Range(0, 1f)] public float snowAmount = 0f;
    public float snowTransitionSpeed = 0.5f;
    [SerializeField] private string snowableTag = "Snowable";
    [SerializeField] private string snowProperty = "_SnowAmount";

    private Renderer[] objRenderers;
    private MaterialPropertyBlock block;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject[] snowableObjects = GameObject.FindGameObjectsWithTag(snowableTag);
        int count = 0;

        foreach (GameObject obj in snowableObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            count += renderers.Length;
        }

        objRenderers = new Renderer[count];
        int index = 0;

        foreach (GameObject obj in snowableObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                objRenderers[index++] = r;
            }
        }

        block = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        float targetSnow = snowAmount;
        foreach (Renderer r in objRenderers)
        {
            r.GetPropertyBlock(block);

            float current = block.GetFloat(snowProperty);
            float value = Mathf.MoveTowards(current, targetSnow, Time.deltaTime * snowTransitionSpeed);

            block.SetFloat(snowProperty, value);
            r.SetPropertyBlock(block);
        }

    }
    public void SetSnow(float target)
    {
        snowAmount = target;
    }
}
