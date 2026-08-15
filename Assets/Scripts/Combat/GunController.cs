using System.Collections;
using UnityEngine;

// Working hit-scan gun. Attach this to your gun model (or any empty object).
//
// Setup:
//  - Put this GameObject under the Main Camera for a first-person viewmodel,
//    or just drop it anywhere in the scene: the script will auto-parent it to
//    the camera at a viewmodel position if it isn't already a camera child.
//  - If the object has no visuals, a grey blocky pistol placeholder is built at
//    runtime so you can start shooting immediately (swap your real model in
//    later by making it a child of this object instead).
//
// Controls:
//  - Hold Left Mouse to fire (automatic). Right-click still does inventory pickup.
public class GunController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to aim/shoot. Auto-found via Camera.main if empty.")]
    public Camera fireCamera;

    [Tooltip("The part that recoils and shows the muzzle flash.")]
    public Transform weaponPivot;

    [Header("Fire")]
    public int damage = 60;
    public float fireRate = 12f;      // shots per second
    public float range = 200f;
    public LayerMask shootMask = ~0;

    [Header("Feel")]
    public float recoilAmount = 0.03f;   // how far the gun kicks back (metres)
    public float recoilRestoreSpeed = 9f;
    public float kickPitch = 0.6f;       // gun jumps up this many degrees

    [Header("Ammo (optional)")]
    public int magazineSize = 30;
    public float reloadTime = 1.4f;
    public bool infiniteAmmo = true;

    [Header("FX")]
    [Tooltip("Seconds a muzzle flash / impact spark stays visible.")]
    public float fxLifetime = 0.08f;

    private int _ammo;
    private bool _reloading;
    private float _nextFireTime;

    private Vector3 _restPos;
    private Quaternion _restRot;

    private Material _metalMat;
    private Material _darkMat;
    private Material _flashMat;
    private Material _sparkMat;

    void Awake()
    {
        if (fireCamera == null) fireCamera = Camera.main;

        // Auto-parent the whole gun to the camera (at a viewmodel position) if
        // the user just dropped it somewhere in the scene instead of dragging
        // it under the Main Camera manually.
        if (fireCamera != null && !transform.IsChildOf(fireCamera.transform))
        {
            transform.SetParent(fireCamera.transform, false);
            transform.localPosition = new Vector3(0.16f, -0.16f, 0.5f);
            transform.localRotation = Quaternion.identity;
        }
        if (weaponPivot == null) weaponPivot = transform;

        // Guard: the recoil must happen on this gun (or a child of it). If the
        // Main Camera / player was dragged into weaponPivot, every shot would
        // kick the whole view back — that feels like the player is being pushed.
        // Revert to the gun itself in that case.
        if (weaponPivot != null && transform.IsChildOf(weaponPivot))
            weaponPivot = transform;

        _restPos = weaponPivot.localPosition;
        _restRot = weaponPivot.localRotation;
        _ammo = magazineSize;

        CreateMaterials();
        if (!HasVisual()) BuildPlaceholderPistol();

        // The gun is a viewmodel: it must NEVER push the player. Any collider
        // or rigidbody on a hand-made gun (rebuilt at runtime OR still in the
        // hierarchy) is disabled here so it can't shove the character around.
        DisableGunPhysics();
    }

    private void DisableGunPhysics()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        // Smoothly return the gun to its resting pose after recoil.
        weaponPivot.localPosition = Vector3.Lerp(weaponPivot.localPosition, _restPos, recoilRestoreSpeed * Time.deltaTime);
        weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, _restRot, recoilRestoreSpeed * Time.deltaTime);

        if (_reloading) return;

        if (Input.GetMouseButton(0)) // Left mouse held to fire
        {
            if (_ammo > 0 || infiniteAmmo)
            {
                if (Time.time >= _nextFireTime) Fire();
            }
            else if (!_reloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }
    }

    private void Fire()
    {
        _nextFireTime = Time.time + 1f / fireRate;
        if (!infiniteAmmo) _ammo--;

        ApplyKick();

        if (fireCamera == null) return;

        Vector3 origin = fireCamera.transform.position;
        Vector3 dir = fireCamera.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, shootMask))
        {
            IDamageable enemy = hit.collider.GetComponentInParent<IDamageable>();
            if (enemy != null)
                enemy.TakeDamage(damage, hit.point, -dir);

            SpawnImpact(hit.point, hit.normal);
        }
        else
        {
            // Tracer end point so you can see where the bullet "goes".
            SpawnImpact(origin + dir * Mathf.Min(range, 60f), -dir);
        }

        SpawnMuzzleFlash();
    }

    private void ApplyKick()
    {
        weaponPivot.localPosition = _restPos + new Vector3(0f, 0f, -recoilAmount);
        weaponPivot.localRotation = _restRot * Quaternion.Euler(-kickPitch, Random.Range(-2f, 2f), 0f);
    }

    // Small bright flash that blinks at the muzzle tip.
    private void SpawnMuzzleFlash()
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flash.name = "MuzzleFlash";
        flash.transform.SetParent(weaponPivot, false);
        flash.transform.localPosition = new Vector3(0f, 0.04f, 0.28f);
        flash.transform.localRotation = Quaternion.identity;
        flash.transform.localScale = new Vector3(0.05f, 0.05f, 0.12f);
        RemoveCollider(flash);
        Renderer r = flash.GetComponent<Renderer>();
        if (r != null) r.material = _flashMat;
        Destroy(flash, fxLifetime);
    }

    private void SpawnImpact(Vector3 point, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spark.name = "BulletImpact";
        spark.transform.position = point + normal * 0.02f;
        spark.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        spark.transform.localScale = Vector3.one * 0.03f;
        RemoveCollider(spark);
        Renderer r = spark.GetComponent<Renderer>();
        if (r != null) r.material = _sparkMat;
        Destroy(spark, fxLifetime);
    }

    // ---------------------------------------------------------------
    // Placeholder so the gun "just works" without any imported model.
    // ---------------------------------------------------------------
    private bool HasVisual()
    {
        return GetComponentInChildren<Renderer>() != null;
    }

    private void BuildPlaceholderPistol()
    {
        // All pieces point along +Z so the barrel faces forward.
        CreatePart("Slide", new Vector3(0.08f, 0.07f, 0.30f), new Vector3(0f, 0.02f, 0.15f), _metalMat);
        CreatePart("Barrel", new Vector3(0.045f, 0.045f, 0.14f), new Vector3(0f, 0.035f, 0.35f), _darkMat);
        CreatePart("FrontSight", new Vector3(0.02f, 0.04f, 0.02f), new Vector3(0f, 0.06f, 0.35f), _darkMat);
        CreatePart("Grip", new Vector3(0.07f, 0.16f, 0.09f), new Vector3(0f, -0.13f, 0.02f), _darkMat);
    }

    private void CreatePart(string name, Vector3 size, Vector3 localPos, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = size;
        RemoveCollider(part);
        Renderer r = part.GetComponent<Renderer>();
        if (r != null) r.material = material;
    }

    private void CreateMaterials()
    {
        _metalMat = MakeMat(new Color(0.30f, 0.32f, 0.35f));
        _darkMat = MakeMat(new Color(0.08f, 0.08f, 0.09f));
        _flashMat = MakeMat(new Color(1.0f, 0.9f, 0.5f));
        _sparkMat = MakeMat(new Color(1.0f, 0.5f, 0.1f));
    }

    private Material MakeMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        return m;
    }

    private void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            // Disabling instantly (rather than waiting for Destroy) means
            // physics NEVER sees these FX colliders. Otherwise a muzzle flash
            // spawning near the player's capsule could shove the player out.
            c.enabled = false;
            Destroy(c);
        }
    }

    private IEnumerator ReloadRoutine()
    {
        _reloading = true;
        yield return new WaitForSeconds(reloadTime);
        _ammo = magazineSize;
        _reloading = false;
    }
}