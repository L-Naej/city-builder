using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
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

    Tilemap _tileMap;

    // Start is called before the first frame update
    void Awake()
    {
        _buildingsList = Resources.LoadAll<BuildingSO>("");
        _uiAssets = Resources.LoadAll<UIAssetsSO>("")[0];

        _playerState = new PlayerState();
        _playerState.state = PlayerState.State.Idle;

        Physics2D.queriesHitTriggers = true;

        _tileMap = FindObjectOfType<Tilemap>();

        Cursor.lockState = CursorLockMode.Confined;
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
                TryPutBuildingOnMap();
            }
        }
    }

    bool IsTileAvailable(Vector3 worldPosition)
    {
        Vector3Int cellPosition = _tileMap.WorldToCell(worldPosition);
        Tile tile = _tileMap.GetTile<Tile>(cellPosition);
        return tile == null || tile.gameObject == null;
    }

    void TryPutBuildingOnMap()
    {
        if (IsPointerOverUIElement()) return;

        Vector3 position = _playerState.selectedBuilding.transform.position;
        position.z = 0;

        if (!IsTileAvailable(position)) return;

        _playerState.state = PlayerState.State.Idle;

        _playerState.selectedBuilding.AddComponent<BoxCollider>();

        BuildingBehavior bb =_playerState.selectedBuilding.AddComponent<BuildingBehavior>();
        bb.building = _playerState.selectedBuildingData;
        _playerState.selectedBuilding.transform.position = position;

        Vector3Int cellPosition = _tileMap.WorldToCell(position);

        Tile tile = _tileMap.GetTile<Tile>(cellPosition);
        if(tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            _tileMap.SetTile(cellPosition, tile);
        }

        tile.gameObject = _playerState.selectedBuilding;

        _playerState.selectedBuilding = null;
        _playerState.selectedBuildingData = null;
    }

    private void UpdateBuildingUnderCursor(GameObject selectedBuilding)
    {
        if(IsPointerOverUIElement())
        {
            selectedBuilding.GetComponent<SpriteRenderer>().enabled = false;
            return;
        }

        selectedBuilding.GetComponent<SpriteRenderer>().enabled = true;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3Int cellPosition = _tileMap.WorldToCell(mousePosition);
        Vector3 cellWorldPosition = _tileMap.GetCellCenterWorld(cellPosition);
        cellWorldPosition.z = Camera.main.nearClipPlane;//If I don't do that, the sprite won't be visible on the screen.

        selectedBuilding.transform.position = cellWorldPosition;

        //Visual feedback if cannot put building here.
        Color buildingColor = IsTileAvailable(cellWorldPosition) ? Color.white : Color.red;
        selectedBuilding.GetComponent<SpriteRenderer>().color = buildingColor;
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

    //From https://forum.unity.com/threads/how-to-detect-if-mouse-is-over-ui.1025533/
    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }


    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }
        return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
