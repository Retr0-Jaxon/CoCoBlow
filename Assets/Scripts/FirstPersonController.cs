using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Crosshair")]
    [SerializeField] private float crosshairSize = 6f;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField] private float dropDistance = 1.25f;
    [SerializeField] private float dropHeightOffset = 0.55f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;
    private HairDryer targetedHairDryer;
    private Texture2D handCursorTexture;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraRoot == null && Camera.main != null)
        {
            cameraRoot = Camera.main.transform;
        }

        handCursorTexture = CreateHandCursorTexture();
    }

    private void OnEnable()
    {
        LockCursor();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (handCursorTexture != null)
        {
            Destroy(handCursorTexture);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null)
        {
            return;
        }

        HandleCursor(keyboard, mouse);
        HandleLook(mouse);
        HandleInteraction(keyboard);
        HandleMovement(keyboard);
    }

    private void HandleCursor(Keyboard keyboard, Mouse mouse)
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        bool showingHand = targetedHairDryer != null && !targetedHairDryer.IsHeld;
        float size = showingHand ? 22f : crosshairSize;
        float halfSize = size * 0.5f;
        Rect crosshair = new Rect((Screen.width * 0.5f) - halfSize, (Screen.height * 0.5f) - halfSize, size, size);

        Color previousColor = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(crosshair, showingHand ? handCursorTexture : Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void HandleInteraction(Keyboard keyboard)
    {
        targetedHairDryer = FindTargetedHairDryer();

        if (keyboard.eKey.wasPressedThisFrame && targetedHairDryer != null && !targetedHairDryer.IsHeld)
        {
            targetedHairDryer.PickUp(cameraRoot);
        }

        if (keyboard.gKey.wasPressedThisFrame)
        {
            HairDryer heldHairDryer = GetComponentInChildren<HairDryer>();
            if (heldHairDryer != null && heldHairDryer.IsHeld)
            {
                DropHairDryer(heldHairDryer);
            }
        }
    }

    private HairDryer FindTargetedHairDryer()
    {
        if (cameraRoot == null || !Physics.Raycast(cameraRoot.position, cameraRoot.forward,
                out RaycastHit hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<HairDryer>();
    }

    private void DropHairDryer(HairDryer hairDryer)
    {
        Vector3 forward = transform.forward;
        Vector3 dropCenter = transform.position + forward * dropDistance + Vector3.up * 2f;
        Vector3 dropPosition = transform.position + forward * dropDistance;

        if (Physics.Raycast(dropCenter, Vector3.down, out RaycastHit groundHit, 4f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            dropPosition = groundHit.point + Vector3.up * dropHeightOffset;
        }

        hairDryer.Drop(dropPosition, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        targetedHairDryer = null;
    }

    private static Texture2D CreateHandCursorTexture()
    {
        string[] pattern =
        {
            "      XX      ",
            "      XX      ",
            "      XX      ",
            "      XX      ",
            "  X   XX      ",
            " XXX  XXX     ",
            " XXXXXXXXX    ",
            " XXXXXXXXXX   ",
            "  XXXXXXXXXX  ",
            "   XXXXXXXXX  ",
            "    XXXXXXX   ",
            "     XXXXX    ",
            "      XXX     ",
            "      XX      "
        };

        Texture2D texture = new Texture2D(pattern[0].Length, pattern.Length, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < pattern.Length; y++)
        {
            for (int x = 0; x < pattern[y].Length; x++)
            {
                texture.SetPixel(x, pattern.Length - 1 - y,
                    pattern[y][x] == 'X' ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return texture;
    }

    private void HandleLook(Mouse mouse)
    {
        if (cameraRoot == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 lookInput = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * lookInput.x);

        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, minPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement(Keyboard keyboard)
    {
        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime * moveSpeed);
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
