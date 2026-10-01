using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Modal Dialog (Pauses Game)")]
    public GameObject modalRoot;
    public Image modalIcon;
    public TMP_Text modalTitle;
    public TMP_Text modalDescription;
    public Button modalDismissButton;

    [Header("Banner / Floating Tooltip")]
    public GameObject bannerRoot;
    public TMP_Text bannerTitle;
    public TMP_Text bannerDescription;

    private Coroutine bannerCoroutine;
    private bool isModalActive = false;

    private const string PREF_CAT = "Tutorial_GhostCat_Seen";
    private const string PREF_WEAPON = "Tutorial_Weapon_Seen";
    private const string PREF_ALTAR = "Tutorial_Altar_Seen";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (modalRoot != null) modalRoot.SetActive(false);
        if (bannerRoot != null) bannerRoot.SetActive(false);

        if (modalDismissButton != null)
        {
            modalDismissButton.onClick.AddListener(DismissModal);
        }
    }

    void Update()
    {
        if (isModalActive)
        {
            bool dismiss = false;
            try { dismiss = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); } catch {}
#if ENABLE_INPUT_SYSTEM
            if (!dismiss && Keyboard.current != null)
            {
                dismiss = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
            }
#endif
            if (dismiss)
            {
                DismissModal();
            }
        }
    }

    public void TriggerGhostCatTutorial()
    {
        if (PlayerPrefs.GetInt(PREF_CAT, 0) == 1) return;
        PlayerPrefs.SetInt(PREF_CAT, 1);
        PlayerPrefs.Save();

        ShowModal(
            "BEWARE: GHOST CAT!",
            "A spectral predator stalks from the foggy trees!\n\n" +
            "• It cannot cross into the sacred Bonfire's light.\n" +
            "• Kite the beast around trees to buy time.\n" +
            "• Touching it is fatal unless you strike with a Holy Weapon!",
            "CONTINUE [SPACE]"
        );
    }

    public void TriggerWeaponTutorial()
    {
        if (PlayerPrefs.GetInt(PREF_WEAPON, 0) == 1) return;
        PlayerPrefs.SetInt(PREF_WEAPON, 1);
        PlayerPrefs.Save();

        ShowBanner(
            "SPIRIT WEAPON ACQUIRED!",
            "Press [SPACE] near a Ghost Cat to strike and banish it!"
        );
    }

    public void TriggerAltarBuffTutorial()
    {
        if (PlayerPrefs.GetInt(PREF_ALTAR, 0) == 1) return;
        PlayerPrefs.SetInt(PREF_ALTAR, 1);
        PlayerPrefs.Save();

        ShowBanner(
            "ALTAR BLESSING AVAILABLE!",
            "Step onto the stone pedestal to absorb divine buffs (Speed, Fury, Cleanse, or Repel)!"
        );
    }

    public void ShowModal(string title, string body, string btnText = "CONTINUE")
    {
        if (modalRoot == null) return;

        isModalActive = true;
        Time.timeScale = 0f;

        if (modalTitle != null) modalTitle.text = title;
        if (modalDescription != null) modalDescription.text = body;
        if (modalDismissButton != null)
        {
            var btnTextComp = modalDismissButton.GetComponentInChildren<TMP_Text>();
            if (btnTextComp != null) btnTextComp.text = btnText;
        }

        modalRoot.SetActive(true);
    }

    public void DismissModal()
    {
        if (!isModalActive) return;

        isModalActive = false;
        if (modalRoot != null) modalRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowBanner(string title, string body, float duration = 4.5f)
    {
        if (bannerRoot == null)
        {
            // Fallback to GameManager notification
            GameManager.Instance?.ShowNotification($"{title}: {body}", duration);
            return;
        }

        if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
        bannerCoroutine = StartCoroutine(BannerRoutine(title, body, duration));
    }

    private IEnumerator BannerRoutine(string title, string body, float duration)
    {
        if (bannerTitle != null) bannerTitle.text = title;
        if (bannerDescription != null) bannerDescription.text = body;
        bannerRoot.SetActive(true);

        yield return new WaitForSeconds(duration);

        bannerRoot.SetActive(false);
        bannerCoroutine = null;
    }

    // Helper for debugging / resetting tutorials
    public static void ResetAllTutorialFlags()
    {
        PlayerPrefs.DeleteKey(PREF_CAT);
        PlayerPrefs.DeleteKey(PREF_WEAPON);
        PlayerPrefs.DeleteKey(PREF_ALTAR);
        PlayerPrefs.Save();
        Debug.Log("Tutorial flags reset.");
    }
}
