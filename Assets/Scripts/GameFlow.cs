using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// Điều phối 5 màn hình (Menu/Map/Combat/KeyGate/End) trong 1 scene, quản lý
/// đội hình: bắt đầu chỉ có Main, tuyển thêm tối đa 3 đồng đội qua Cổng Chìa Khóa.
public class GameFlow : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject mapPanel;
    public GameObject combatPanel;
    public GameObject keyGatePanel;
    public GameObject endPanel;

    public Button startButton;
    public Button restartButton;
    public Button keyGateContinueButton;
    public Text keyGateLoreText;
    public Text endTitleText;

    public MapManager mapManager;
    public CombatManager combatManager;

    public GameObject mainHeroPrefab;
    public GameObject[] recruitOrder;
    public string[] recruitLoreLines;

    readonly List<GameObject> activeParty = new List<GameObject>();
    int recruitIndex;

    void Start()
    {
        startButton.onClick.AddListener(OnStart);
        restartButton.onClick.AddListener(OnRestart);
        keyGateContinueButton.onClick.AddListener(OnKeyGateContinue);

        mapManager.onCombatGate = OnCombatGate;
        mapManager.onKeyGate = OnKeyGate;

        ShowOnly(mainMenuPanel);
    }

    void OnStart()
    {
        activeParty.Clear();
        activeParty.Add(mainHeroPrefab);
        recruitIndex = 0;
        mapManager.Setup();
        ShowOnly(mapPanel);
    }

    void OnCombatGate(GameObject enemyPrefab, bool isBoss)
    {
        ShowOnly(combatPanel);
        combatManager.StartCombat(activeParty.ToArray(), enemyPrefab, OnCombatEnd);
    }

    void OnCombatEnd(bool won)
    {
        if (!won)
        {
            ShowEnd(false);
            return;
        }
        if (mapManager.IsAtFinalBoss)
        {
            ShowEnd(true);
            return;
        }
        mapManager.AdvanceStage();
        ShowOnly(mapPanel);
    }

    void OnKeyGate()
    {
        keyGateLoreText.text = recruitIndex < recruitLoreLines.Length
            ? recruitLoreLines[recruitIndex]
            : "You find only silence and dust here.";
        ShowOnly(keyGatePanel);
    }

    void OnKeyGateContinue()
    {
        if (recruitIndex < recruitOrder.Length)
        {
            activeParty.Add(recruitOrder[recruitIndex]);
            recruitIndex++;
        }
        mapManager.AdvanceStage();
        ShowOnly(mapPanel);
    }

    void ShowEnd(bool victory)
    {
        endTitleText.text = victory ? "VICTORY!\nYou have escaped the Shattered Gate." : "DEFEAT";
        ShowOnly(endPanel);
    }

    void OnRestart()
    {
        ShowOnly(mainMenuPanel);
    }

    void ShowOnly(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        mapPanel.SetActive(panel == mapPanel);
        combatPanel.SetActive(panel == combatPanel);
        keyGatePanel.SetActive(panel == keyGatePanel);
        endPanel.SetActive(panel == endPanel);
    }
}
