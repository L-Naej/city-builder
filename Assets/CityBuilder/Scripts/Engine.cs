using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Engine : MonoBehaviour
{
    public class PlayerController
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
        private ResourceSO _generatedResource;
        
        public void Update(float deltaTime)
        {
            _elapsedTimeSinceLastTimeGenerated += deltaTime;
            if(_elapsedTimeSinceLastTimeGenerated >= generator.generationRate)
            {
                player.resourcesAmount[generator.resourceType] += generator.generationAmount;
                _elapsedTimeSinceLastTimeGenerated = 0;
            }
        }
    }

    protected PlayerSO _playerState;
    protected BuildingSO[] _buildingsList;
    protected UIAssetsSO _uiAssets;
    protected ResourceSO[] _resourcesList;

    protected PlayerController _playerController;

    protected Tilemap _tileMap;

    protected List<Generator> _generatorsOnMap;

    protected TMPro.TextMeshProUGUI _resourcesDisplayer;

    void Awake()
    {
        _playerState = Resources.LoadAll<PlayerSO>("")[0];
        _buildingsList = Resources.LoadAll<BuildingSO>("");
        _uiAssets = Resources.LoadAll<UIAssetsSO>("")[0];
        _resourcesList = Resources.LoadAll<ResourceSO>("");

        _playerState.InitResources(_resourcesList);

        _playerController = new PlayerController();
        _playerController.state = PlayerController.State.Idle;

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
        if(_playerController.state == PlayerController.State.SelectedBuildingInUI)
        {
            UpdateBuildingUnderCursor(_playerController.selectedBuilding, _playerController.selectedBuildingData);

            if(Input.GetMouseButtonDown(0))
            {
                if(TryPutBuildingOnMap(_playerController.selectedBuilding, _playerController.selectedBuildingData))
                {
                    UnselectBuilding();
                }
            }
            else if(Input.GetMouseButtonDown(1))
            {
                Destroy(_playerController.selectedBuilding);
                UnselectBuilding();
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
        StringBuilder resourcesStringBuilder = new StringBuilder();
        foreach (KeyValuePair<ResourceSO, int> resourceAmount in _playerState.resourcesAmount)
        {
            resourcesStringBuilder.Append($"{resourceAmount.Key.name}: {resourceAmount.Value} | ");
        }
        _resourcesDisplayer.text = resourcesStringBuilder.ToString();
    }

    bool IsTileAvailable(Vector3 worldPosition)
    {
        Vector3Int cellPosition = _tileMap.WorldToCell(worldPosition);
        Tile tile = _tileMap.GetTile<Tile>(cellPosition);
        return tile == null || tile.gameObject == null;
    }

    bool CanPlayerPayForThisBuilding(BuildingSO building)
    {
        return _playerState.resourcesAmount[building.Cost.Resource] >= building.Cost.Amount;
    }

    bool TryPutBuildingOnMap(GameObject selectedBuilding, BuildingSO selectedBuildingData)
    {
        // All checks to see if it's possible to put the building on the map
        if (IsPointerOverUIElement()) return false;

        Vector3 position = selectedBuilding.transform.position;
        position.z = 0;

        if (!CanPlayerPayForThisBuilding(selectedBuildingData)) return false;
        if (!IsTileAvailable(position)) return false;

        //All clear, actually put the building on the map

        selectedBuilding.AddComponent<BoxCollider>();

        BuildingBehavior bb = selectedBuilding.AddComponent<BuildingBehavior>();
        bb.building = selectedBuildingData;
        selectedBuilding.transform.position = position;

        Vector3Int cellPosition = _tileMap.WorldToCell(position);

        Tile tile = _tileMap.GetTile<Tile>(cellPosition);
        if(tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            _tileMap.SetTile(cellPosition, tile);
        }

        tile.gameObject = selectedBuilding;

        //If it's a generator, register it so we can make it "tick" later.
        if(selectedBuildingData is GeneratorSO)
        {
            Generator generator = new Generator();
            generator.generator = selectedBuildingData as GeneratorSO;
            generator.player = _playerState;
            _generatorsOnMap.Add(generator);
        }

        //Remove cost from player
        _playerState.resourcesAmount[selectedBuildingData.Cost.Resource] -= selectedBuildingData.Cost.Amount;

        return true;
    }

    private void UpdateBuildingUnderCursor(GameObject selectedBuilding, BuildingSO selectedBuildingData)
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
        Color buildingColor =  IsTileAvailable(cellWorldPosition) 
                            && CanPlayerPayForThisBuilding(selectedBuildingData) ? 
                            Color.white : Color.red;
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
        _playerController.state = PlayerController.State.SelectedBuildingInUI;

        GameObject building = new GameObject(selectedBuilding.Name);
        SpriteRenderer buildingSprite = building.AddComponent<SpriteRenderer>();
        buildingSprite.sprite = selectedBuilding.WorldSprite;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        building.transform.position = Camera.main.ScreenToWorldPoint(mousePosition);

        _playerController.selectedBuilding = building;
        _playerController.selectedBuildingData = selectedBuilding;
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

    void UnselectBuilding()
    {
        _playerController.state = PlayerController.State.Idle;
        _playerController.selectedBuilding = null;
        _playerController.selectedBuildingData = null;
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
