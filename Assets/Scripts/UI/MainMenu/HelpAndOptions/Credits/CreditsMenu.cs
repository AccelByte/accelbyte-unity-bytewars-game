using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsMenu : MenuCanvas
{
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Button backButton;
    [SerializeField] private CreditsData[] creditsNames;

    private float waitTime = 1f;
    private float scrollSpeed = 0.125f;

    private void Start()
    {
        // UI Initialization
        backButton.onClick.AddListener(() => MenuManager.Instance.OnBackPressed());
        
        DisplayCredits();
    }

    private void Update()
    {
        ScrollCredits(Time.deltaTime);
    }

    private void OnDisable()
    {
        // reset auto-scroll
        waitTime = 1f;
        scrollView.verticalNormalizedPosition = 1f;
    }

    private void ScrollCredits(float deltaTime)
    {
        if (ShouldWait())
        {
            waitTime -= 1f * deltaTime;
            return;
        }

        if (HasReachedBottom())
        {
            return;
        }
        
        scrollView.verticalNormalizedPosition -= scrollSpeed * deltaTime;
    }

    private void DisplayCredits()
    {
        // Get Prefab
        GameObject roleGroupPanelPrefab = AssetManager.Singleton.GetAsset(AssetEnum.RoleGroupPanel) as GameObject;
        GameObject memberNameTextPrefab = AssetManager.Singleton.GetAsset(AssetEnum.MemberNameText) as GameObject;

        foreach (CreditsData creditData in creditsNames)
        {
            GameObject roleGroupPanel = Instantiate(roleGroupPanelPrefab, scrollView.content);
            TMP_Text roleGroupNameText = roleGroupPanel.GetComponentInChildren<TMP_Text>();
            roleGroupNameText.text = creditData.roleGroupName;

            foreach (string memberName in creditData.memberNames)
            {
                GameObject memberNameTextObject = Instantiate(memberNameTextPrefab, roleGroupPanel.transform);
                TMP_Text memberNameText = memberNameTextObject.GetComponent<TMP_Text>();
                memberNameText.text = memberName;
            }
        }
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreditsMenuCanvas;
    }

    private bool HasReachedBottom() => scrollView.verticalNormalizedPosition <= 0;
    private bool ShouldWait() => waitTime > 0;
}
