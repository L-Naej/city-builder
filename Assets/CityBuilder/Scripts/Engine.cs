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

    public class Generator
    {
        public GeneratorSO generator;
        public PlayerSO player;

        private float _elapsedTimeSinceLastTimeGenerated = 0;
        
        public void Update(float deltaTime)
        {
            _elapsedTimeSinceLastTimeGenerated += deltaTime;
            if(_elapsedTimeSinceLastTimeGenerated >= generator.generationRate)
            {
                player.resources += generator.generationAmount;
                _elapsedTimeSinceLastTimeGenerated = 0;
            }
        }
    }

    protected PlayerSO _playerData;
    protected BuildingSO[] _buildingsList;
    protected UIAssetsSO _uiAssets;

    protected PlayerState _playerState;

    protected Tilemap _tileMap;

    protected List<Generator> _generatorsOnMap;

    protected TMPro.TextMeshProUGUI _resourcesDisplayer;

    // Start is called before the first frame update
    void Awake()
    {
        _playerData = Resources.LoadAll<PlayerSO>("")[0];
        _buildingsList = Resources.LoadAll<BuildingSO>("");
        _uiAssets = Resources.LoadAll<UIAssetsSO>("")[0];

        _playerState = new PlayerState();
        _playerState.state = PlayerState.State.Idle;

        Physics2D.queriesHitTriggers = true;

        _tileMap = FindObjectOfType<Tilemap>();

        _generatorsOnMap = new List<Generator>();

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

        UpdateGenerators();
        UpdateResourcesUI();
    }

    void UpdateGenerators()
    {
        foreach(Generator generator in _generatorsOnMap)
        {
            generator.Update(Time.deltaTime);
        }
    }

    void UpdateResourcesUI()
    {
        _resourcesDisplayer.text = _playerData.resources.ToString();
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

        //If it's a generator, register it so we can make it "tick" later.
        if(_playerState.selectedBuildingData is GeneratorSO)
        {
            Generator generator = new Generator();
            generator.generator = _playerState.selectedBuildingData as GeneratorSO;
            generator.player = _playerData;
            _generatorsOnMap.Add(generator);
        }

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
        //Instantiate buildings UI
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

        //Instantiate Resource Displayer UI
        GameObject resourcesUI = Instantiate(_uiAssets.UIResourcesDisplayer);
        _resourcesDisplayer = resourcesUI.transform.Find("ResourcesTab").Find("lblResourcesAmount").GetComponent<TMPro.TextMeshProUGUI>();
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
