using System.Collections;
using UnityEngine;

// Shootable NPC / dummy / enemy. Put this on any GameObject that has a Collider
// (one is auto-added if missing) and it will take damage from GunController,
// flash white while hit and shrink away when it dies.
//
// If the object has no renderers a simple capsule dummy (body + head) is built
// at runtime so you can test immediately — swap in your own model later.
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Death")]
    [Tooltip("Empty = stays until the end of the match. Otherwise auto-removes body after this delay.")]
    public float autoDestroyDelay = -1f;

    private int _hp;
    private Renderer[] _renderers;
    private Color[] _baseColors;
    private bool _dead;

    public bool IsDead => _dead;

    void Awake()
    {
        _hp = maxHealth;
        _renderers = GetComponentsInChildren<Renderer>(true);

        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material m = _renderers[i].material;
            if (m.HasProperty("_BaseColor")) _baseColors[i] = m.GetColor("_BaseColor");
            else if (m.HasProperty("_Color")) _baseColors[i] = m.GetColor("_Color");
            else _baseColors[i] = Color.white;
        }

        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<CapsuleCollider>();

        if (_renderers.Length == 0) BuildPlaceholderDummy();
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_dead || amount <= 0) return;

        _hp = Mathf.Max(0, _hp - amount);
        StopAllCoroutines();
        StartCoroutine(HitFlashRoutine());

        if (_hp <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        SetAllColors(Color.white);
        yield return new WaitForSeconds(0.08f);
        if (!_dead) RestoreColors();
    }

    private void Die()
    {
        _dead = true;

        // Can't be shot again once dead.
        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        foreach (var r in _renderers)
        {
            Material m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.4f, 0.1f, 0.1f));
            else if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.4f, 0.1f, 0.1f));
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Quick "crunch down" shrink instead of popping out instantly.
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            transform.localScale = new Vector3(1, 1f - p, 1);
            yield return null;
        }
        transform.localScale = new Vector3(1, 0f, 1);

        if (autoDestroyDelay > 0f)
            Destroy(gameObject, autoDestroyDelay);
    }

    private void SetAllColors(Color c)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material m = _renderers[i].material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material m = _renderers[i].material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", _baseColors[i]);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", _baseColors[i]);
        }
    }

    // Builds a simple grey capsule dummy if no model was assigned.
    private void BuildPlaceholderDummy()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        Color c = new Color(0.55f, 0.30f, 0.30f); // nice "target" red
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
        DestroyC(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().material = mat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(transform, false);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        head.transform.localScale = Vector3.one * 0.32f;
        DestroyC(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = mat;

        // Re-grab renderers for hit flashes.
        _renderers = GetComponentsInChildren<Renderer>(true);
        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++) _baseColors[i] = c;
    }

    private void DestroyC(Collider collider)
    {
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }
}