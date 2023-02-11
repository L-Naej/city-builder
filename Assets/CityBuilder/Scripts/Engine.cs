using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Engine : MonoBehaviour
{
    public class PlayerState
    {
        public enum State
        {
            Idle,
            SelectedBuildingInUI
        }

        public State state;

        public GameObject selectedBuilding;
        public BuildingSO selectedBuildingData;
    }
    
    BuildingSO[] _buildingsList;
    UIAssetsSO _uiAssets;

    PlayerState _playerState;

    // Start is called before the first frame update
    void Awake()
    {
        _buildingsList = Resources.LoadAll<BuildingSO>("");
        _uiAssets = Resources.LoadAll<UIAssetsSO>("")[0];

        _playerState = new PlayerState();
        _playerState.state = PlayerState.State.Idle;

        Physics2D.queriesHitTriggers = true;
    }

    private void Start()
    {
        InstantiateUI();
    }

    private void Update()
    {
        if(_playerState.state == PlayerState.State.SelectedBuildingInUI)
        {
            UpdateBuildingUnderCursor(_playerState.selectedBuilding);

            if(Input.GetMouseButtonDown(0))
            {
                PutBuildingOnMap();
            }
        }
    }

    void PutBuildingOnMap()
    {
        _playerState.state = PlayerState.State.Idle;

        Collider collider = _playerState.selectedBuilding.AddComponent<BoxCollider>();

        BuildingBehavior bb =_playerState.selectedBuilding.AddComponent<BuildingBehavior>();
        bb.building = _playerState.selectedBuildingData;
        Vector3 position = _playerState.selectedBuilding.transform.position;
        position.z = 0;
        _playerState.selectedBuilding.transform.position = position;

        _playerState.selectedBuilding = null;
        _playerState.selectedBuildingData = null;
    }

    private void UpdateBuildingUnderCursor(GameObject selectedBuilding)
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        selectedBuilding.transform.position = Camera.main.ScreenToWorldPoint(mousePosition);
    }

    private void InstantiateUI()
    {
        GameObject ui = Instantiate(_uiAssets.UIBuildingsList);
        HorizontalLayoutGroup buttonsContainer = ui.GetComponentInChildren<HorizontalLayoutGroup>();
        ui.GetComponent<Canvas>().worldCamera = Camera.main;

        foreach(BuildingSO building in _buildingsList)
        {
            GameObject buttonGO = Instantiate(_uiAssets.UIBuildingButton, buttonsContainer.transform);
            Button button = buttonGO.GetComponentInChildren<Button>();
            var lblName = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            lblName.text = building.Name;

            
            Image img = button.transform.Find("Icon").GetComponent<Image>();
            img.sprite = building.UISprite;

            button.onClick.AddListener(() => OnSelectedBuildingInUI(building));
        }
    }

    private void OnSelectedBuildingInUI(BuildingSO selectedBuilding)
    {
        _playerState.state = PlayerState.State.SelectedBuildingInUI;

        GameObject building = new GameObject(selectedBuilding.Name);
        SpriteRenderer buildingSprite = building.AddComponent<SpriteRenderer>();
        buildingSprite.sprite = selectedBuilding.WorldSprite;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        building.transform.position = Camera.main.ScreenToWorldPoint(mousePosition);

        _playerState.selectedBuilding = building;
        _playerState.selectedBuildingData = selectedBuilding;
    }
}
