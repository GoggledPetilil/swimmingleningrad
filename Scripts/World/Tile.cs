using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Data")]
    public string m_TileName;
    public bool m_Walkable; // If unoccupied, can units walk on this tile?
    public UnitBase m_OccupiedUnit;
    public bool m_IsWalkable => m_Walkable && m_OccupiedUnit == null;
    public int m_TravelCost;
    public int m_DefBoost;
    private UnitBase.Faction movingFaction;

    [Header("Range Data")]
    public List<Tile> m_AdjacentList = new List<Tile>();
    public bool m_visited;
    public Tile m_LastTile;
    public int m_Distance = 0;

    [Header("Enemy AI Data")]
    public float f = 0f;
    public float g = 0f;
    public float h = 0f;

    [SerializeField] private SpriteRenderer m_sr;

    void Start()
    {
        Init();
    }

    protected void Init()
    {
        m_sr.enabled = false;
    }

    public void SetUnit(UnitBase unit)
    {
        if(unit.m_Occupying != null) unit.m_Occupying.m_OccupiedUnit = null; // Unit won't occupy their last tile anymore.
        unit.transform.position = transform.position;
        unit.m_Occupying = this;
        m_OccupiedUnit = unit;
    }

    public virtual void TileFunction()
    {
        
    }

    void OnMouseEnter()
    {
        UnitBase u = UnitManager.m_instance.m_SelectedUnit;
        if(GridManager.m_instance.m_CanClick == false ||
        (u != null && u.m_IsAttacking && !u.m_TileRange.Contains(this))) return;

        GridManager.m_instance.SetCursorPosition(transform.position);
        SoundManager.m_instance.PlayAudio(SoundManager.m_instance.m_Cursor);

        CameraManager.m_instance.SetCameraTarget(this.gameObject.transform.position);

        GridManager.m_instance.ShowHighlightedUnit(m_OccupiedUnit);
        GridManager.m_instance.ShowHighlightedTile(this);
    }

    void OnMouseExit()
    {
        if(GridManager.m_instance.m_CanClick == false) return;

        GridManager.m_instance.SetCursorPosition(new Vector2(99, 99));
        GridManager.m_instance.ShowHighlightedUnit(null);
        GridManager.m_instance.ShowHighlightedTile(null);
    }

    public virtual void OnMouseDown()
    {
        // You can't click on tiles if you don't have permission to lol
        if(GridManager.m_instance.m_CanClick == false) return;

        SoundManager.m_instance.PlayAudio(SoundManager.m_instance.m_Confirm);
        if(TurnManager.m_instance.m_Phase == TurnManager.Phase.PlayerPhase)
        {
            movingFaction = UnitBase.Faction.Hero;
        }
        else
        {
            movingFaction = UnitBase.Faction.Enemy;
        }

        if(m_OccupiedUnit != null)
        {
            // This tile is occupied. Interact with the occupent:
            if(m_OccupiedUnit.m_Faction == movingFaction)
            {
                // Occupying unit is your unit.
                if(m_OccupiedUnit.m_Hasmoved == false)
                {
                    // Your unit hasn't moved yet.
                    if(m_OccupiedUnit != UnitManager.m_instance.m_SelectedUnit)
                    {
                        // This is a different hero, so select this unit instead.
                        SelectThisHero();
                    }
                    else
                    {
                        // The occupying unit is the same as the unit already selected.
                        m_OccupiedUnit.FinishedMoving();
                        GridManager.m_instance.TileClickAllowed(false);
                        GridManager.m_instance.ToggleCursor(false);
                    }
                }
                else
                {
                    // The hero has already moved.
                    SelectThisHero();
                }

            }
            else
            {
                // Occupying unit is an enemy.
                if(UnitManager.m_instance.m_SelectedUnit == null || UnitManager.m_instance.m_SelectedUnit.m_IsAttacking == false)
                {
                    // The player is not trying to attack this enemy, so display range.
                    SelectThisHero();
                }
                else if(UnitManager.m_instance.m_SelectedUnit.m_SelectableEnemies.Contains(m_OccupiedUnit))
                {
                    // The player has an attacking unit selected, attack this enemy.
                    if(movingFaction == UnitBase.Faction.Hero)
                    {
                        BattleManager.m_instance.StartBattle((Hero)UnitManager.m_instance.m_SelectedUnit, (Enemy)m_OccupiedUnit);
                    }
                    else
                    {
                        BattleManager.m_instance.StartBattle((Enemy)UnitManager.m_instance.m_SelectedUnit, (Hero)m_OccupiedUnit);
                    }

                    if(TurnManager.m_instance.m_Phase == TurnManager.Phase.PlayerPhase || GameManager.m_instance.m_IsMultiplayer)
                    {
                        MenuManager.m_instance.ToggleEndButton(true);
                    }
                }
            }
        }
        else
        {
            // This tile is free.
            if(UnitManager.m_instance.m_SelectedUnit != null)
            {
                // The Player has a unit selected
                UnitBase unit = UnitManager.m_instance.m_SelectedUnit;

                if(unit.m_TileRange.Contains(this) && unit.m_Faction == movingFaction &&
                !unit.m_Hasmoved && !unit.m_Moving && !unit.m_IsAttacking)
                {
                    // The selected Player unit will now move to this tile.
                    UnitManager.m_instance.m_SelectedUnit.MoveToTile(this);
                    GridManager.m_instance.TileClickAllowed(false);
                    GridManager.m_instance.ToggleCursor(false);
                }
                else if(UnitManager.m_instance.m_SelectedUnit.m_IsAttacking == false)
                {
                    // Player clicked a tile out of range or had an enemy selected, so deselect the unit.
                    UnitManager.m_instance.SetSelectedHero(null);
                    MenuManager.m_instance.ToggleEndButton(true);
                }
            }
        }
    }

    void SelectThisHero()
    {
        if(m_OccupiedUnit.m_Faction == UnitBase.Faction.Hero)
        {
            UnitManager.m_instance.SetSelectedHero((Hero)m_OccupiedUnit);
        }
        else if(m_OccupiedUnit.m_Faction == UnitBase.Faction.Enemy)
        {
            UnitManager.m_instance.SetSelectedHero((Enemy)m_OccupiedUnit);
        }
    }

    public void ChangeColor(Color c)
    {
        m_sr.enabled = true;
        m_sr.color = c;
    }

    public void FindNeighbours(Tile target, bool ignoreOccupied)
    {
        Reset();
        CheckTile(Vector2.up, target, ignoreOccupied);
        CheckTile(Vector2.down, target, ignoreOccupied);
        CheckTile(Vector2.right, target, ignoreOccupied);
        CheckTile(-Vector2.right, target, ignoreOccupied);
    }

    public void Reset()
    {
        m_visited = false;
        m_LastTile = null;
        m_Distance = 0;
        m_AdjacentList.Clear();
        m_sr.enabled = false;

        f = g = h = 0f;
    }

    public void CheckTile(Vector2 direction, Tile target, bool ignoreOccupied)
    {
        Vector2 size = new Vector2(0.25f, 0.25f);
        Vector3 p = new Vector3(transform.position.x + direction.x, transform.position.y + direction.y, transform.position.z);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(p, size, 0f);

        foreach(Collider2D item in colliders)
        {
            Tile tile = item.GetComponent<Tile>();
            if(tile != null && (tile.m_IsWalkable && ignoreOccupied || !ignoreOccupied) || tile == target)
            {
                m_AdjacentList.Add(tile);
            }
        }
    }
}
