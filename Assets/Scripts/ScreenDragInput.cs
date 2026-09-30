using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Déplacement TACTILE "au doigt glisse partout sur l'écran" :
/// le doigt touche n'importe où → on glisse et le slime part dans cette
/// direction. PAS de bouton ni de joystick à l'écran.
///
/// S'expose via la propriété `Direction` (Vector2 -1..1) : GameManager la
/// distribue à SlimeController exactement comme avant (jouable aussi à la
/// souris sur PC pour tester, et toujours au clavier WASD/flèches).
/// </summary>
public class ScreenDragInput : MonoBehaviour
{
    [Tooltip("Nombre de pixels de doigt pour atteindre la vitesse maximum")]
    public float maxDragPixels = 150f;

    /// <summary>Direction de poussée (-1..1). Lecture via reflection par SlimeController.</summary>
    public Vector2 Direction { get; private set; }

    /// <summary>true = un TAP (appui court sans glisser) en attente de saut.
    /// Consommé par SlimeController via ConsumeJump() (reflection).</summary>
    public bool JumpRequested { get; private set; }

    private Vector2 dragOrigin;   // où le doigt a touché l'écran
    private Vector2 pressPos;     // départ du TAP (pour mesurer le glissement)
    private float pressTime;      // heure de début du TAP
    private float jumpExpires;    // le tap expire s'il n'est pas consommé
    private int fingerId = -1;    // quel doigt contrôle le mouvement
    private bool mouseDragging;   // test éditeur PC avec la souris

    private void Update()
    {
        Direction = Vector2.zero;

        if (Input.touchCount > 0)
            HandleTouches();

        HandleMouseForEditor();

        // Saut au clavier aussi (test PC)
        if (Input.GetKeyDown(KeyCode.Space)) QueueJump();

        // Un tap non consommé expire rapidement
        if (JumpRequested && Time.time > jumpExpires) JumpRequested = false;
    }

    /// <summary>Demande un saut (valide 0,3 s — consommé à la réception au sol).</summary>
    private void QueueJump()
    {
        JumpRequested = true;
        jumpExpires = Time.time + 0.3f;
    }

    /// <summary>Appelé par SlimeController (reflection) : true une seule fois par tap.</summary>
    public bool ConsumeJump()
    {
        if (!JumpRequested) return false;
        JumpRequested = false;
        return true;
    }

    // ────────────────────────────────────────────────────────────────
    //  DOIGTS (mobile)
    // ────────────────────────────────────────────────────────────────
    private void HandleTouches()
    {
        foreach (Touch touch in Input.touches)
        {
            // Le doigt appui : on prend le contrôle si pas sur un bouton UI
            if (touch.phase == TouchPhase.Began)
            {
                if (fingerId == -1 && !IsOverUI(touch.fingerId))
                {
                    fingerId = touch.fingerId;
                    dragOrigin = touch.position;
                    pressPos = touch.position;
                    pressTime = Time.time;
                }
                continue;
            }

            if (touch.fingerId != fingerId) continue;

            switch (touch.phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Direction = DirFromDrag(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    fingerId = -1;   // doigt relevé : le slime s'arrête
                    // TAP = appui court (< 0,25 s) sans glissement (~20 px)
                    if (Time.time - pressTime < 0.25f &&
                        (touch.position - pressPos).sqrMagnitude < 400f)
                        QueueJump();
                    break;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  SOURIS (marche AUSSI si le projet cible Android/iOS : sur un vrai
    //  téléphone, aucun clic souris n'existe donc ça reste sans effet)
    // ────────────────────────────────────────────────────────────────
    private void HandleMouseForEditor()
    {

        if (Input.GetMouseButtonDown(0) && !IsOverUI(-1))
        {
            mouseDragging = true;
            dragOrigin = Input.mousePosition;
            pressPos = Input.mousePosition;
            pressTime = Time.time;
        }
        else if (Input.GetMouseButton(0) && mouseDragging)
        {
            Direction = DirFromDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
            Direction = Vector2.zero;
            // Clic bref sans bouger = SAUT
            if (!IsOverUI(-1) && Time.time - pressTime < 0.25f &&
                ((Vector2)Input.mousePosition - pressPos).sqrMagnitude < 400f)
                QueueJump();
        }
    }

    /// <summary>Convertit le glissement du doigt en direction (-1..1).</summary>
    private Vector2 DirFromDrag(Vector2 current)
    {
        Vector2 delta = (current - dragOrigin) / maxDragPixels;
        // Petit point de départ doux : il faut glisser ~5 px pour bouger
        if (delta.magnitude < 0.03f) return Vector2.zero;

        return Vector2.ClampMagnitude(delta, 1f);
    }

    /// <summary>true = le doigt est sur un bouton de l'interface (on ignore).</summary>
    private bool IsOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}