﻿using System;
using System.Collections.Generic;
using System.Linq;
using Settworks.Hexagons;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    #region Fields

    private static float _hexScaleFactor;
    private static Vector3 _startPos;

    [SerializeField]
    private BoxCollider _board;

    /// <summary>
    /// Calculated from width.
    /// </summary>
    private int _heightInHexes;

    private HexTile[][] _hexGrid;

    [SerializeField]
    private GameObject _hexPrefab;

    [SerializeField, Range(0, 1)]
    private float _roadBlockChance;

    private Transform _transform;

    [SerializeField]
    private int _widthInHexes;

    #endregion

    #region Properties

    /// <summary>
    /// Calculated from width.
    /// </summary>
    public int HeightInHexes
    {
        get { return _heightInHexes; }
    }

    public int WidthInHexes
    {
        get { return _widthInHexes; }
    }

    public BoxCollider Board
    {
        get { return _board; }
    }

    #endregion

    #region Other Members

    // Use this for initialization
    private void Start()
    {
        _transform = transform;

        GenerateHexGrid();
    }

    /// <summary>
    /// Randomly generate a hex grid that would fill the given BoxCollider.
    /// </summary>
    private void GenerateHexGrid()
    {
        if (_hexPrefab == null) throw new ArgumentNullException("_prefab");
        if (Board == null) throw new ArgumentNullException("_board");

        float boardWidth = Board.bounds.size.x;
        float boardHeight = Board.bounds.size.z;
        float gameObjectScale = (boardWidth / WidthInHexes);

        //Multiply with height to width ratio.
        float sideToSideSize = gameObjectScale / (Mathf.Sqrt(3) / 2f);
        _hexPrefab.transform.localScale = Vector3.one * (sideToSideSize);

        //Size of a single side
        float sideSize = _hexPrefab.transform.localScale.z / 2f;

        //Save hex scale factor for later use.
        _hexScaleFactor = 1f / sideSize;

        //2 Vertical hexagons coupled together for ease of calculation.
        float totalHexHeightCoupled = Mathf.RoundToInt(boardHeight / (sideSize * 3));

        //Find total hexagon count.
        _heightInHexes = (int)(totalHexHeightCoupled * 2);

        //Find total hexagon height. 1 for odds, 2 for evens.
        float totalHexHeight = totalHexHeightCoupled * sideSize * 3;

        //Clip overflow.
        if (totalHexHeight > boardHeight)
        {
            if (HeightInHexes % 2 != 0) totalHexHeight -= sideSize * 2;
            else totalHexHeight -= sideSize;
            _heightInHexes = HeightInHexes - 1;
        }

        //Center the grid vertically.
        float verticalPadding = Mathf.Max(0, (boardHeight - totalHexHeight) / 2f);

        //Offset grid to being from the corner instead of center.
        _startPos = _transform.position -
            new Vector3((boardWidth - gameObjectScale) * 0.5f,
                0,
                (boardHeight - sideToSideSize) * 0.5f - verticalPadding);

        _hexGrid = new HexTile[HeightInHexes][];

        for (int r = 0; r < HeightInHexes; r++)
        {
            //Prevent overflow on odd columns.
            int tempWidth = r % 2 == 0 ? WidthInHexes : WidthInHexes - 1;
            _hexGrid[r] = new HexTile[tempWidth];
            for (int q = 0; q < tempWidth; q++)
            {
                //Scale and position Hex.
                HexCoord newHexCoord = new HexCoord(q - (r - (r & 1)) / 2, r);

                //Spawn and parent Hex.
                GameObject spawnedHex =
                    Instantiate(_hexPrefab, GetWorldPositionOfHex(newHexCoord), Quaternion.identity) as GameObject;
                spawnedHex.transform.parent = _transform;

                //Initialize Hex.
                HexTile hexTile = spawnedHex.GetComponent<HexTile>();
                bool isPassable;
                //An easy way to make sure board is traversable from side to side.
                if (newHexCoord.Neighbors().Where(IsCordinateValid).Any(tile =>
                {
                    HexTile hexTileTemp = GetHexTile(tile);
                    return hexTileTemp != null && !hexTileTemp.IsPassable;
                }))
                {
                    isPassable = true;
                }
                else
                {
                    isPassable = !(UnityEngine.Random.value < _roadBlockChance && r > 1 && HeightInHexes - r > 2);
                }
                hexTile.SetCoord(newHexCoord, isPassable);
                _hexGrid[r][q] = hexTile;
            }
        }
    }

    /// <summary>
    /// Return lenght of given row.
    /// </summary>
    /// <param name="r"></param>
    public int GetRowLenght(int r)
    {
        return _hexGrid[r].Length;
    }

    /// <summary>
    /// Return lenght of given column. Equal to HeightInHexes
    /// </summary>
    public int GetColumnLenght()
    {
        return _heightInHexes;
    }

    /// <summary>
    /// Converts HexCoord to world position according to this grid.
    /// </summary>
    /// <param name="hexCoord"></param>
    /// <returns></returns>
    public Vector3 GetWorldPositionOfHex(HexCoord hexCoord)
    {
        Vector2 hexPos = hexCoord.Position();
        return _startPos + new Vector3(hexPos.x, 0, hexPos.y) / _hexScaleFactor;
    }

    /// <summary>
    /// This finds and uses actual array indexes and checks their validity.
    /// </summary>
    public bool IsCordinateValid(HexCoord hexCoord)
    {
        return IsCordinateValid(hexCoord.q, hexCoord.r);
    }

    /// <summary>
    /// This finds and uses actual array indexes and checks their validity.
    /// </summary>
    public bool IsCordinateValid(int q, int r)
    {
        if (_hexGrid.Length > 0 && r >= 0 && r < _hexGrid.Length)
        {
            int calculatedQ = (q + r / 2);
            return (_hexGrid[r] != null && _hexGrid[r].Length > 0 && calculatedQ >= 0
                && calculatedQ < _hexGrid[r].Length);
        }
        return false;
    }

    /// <summary>
    /// This finds actual data in grid array.
    /// <param name="hexCoord">HexTile coordinates.</param>
    /// </summary>
    public HexTile GetHexTile(HexCoord hexCooord)
    {
        return GetHexTile(hexCooord.q, hexCooord.r);
    }

    /// <summary>
    /// This finds actual data in grid array.
    /// q and r are HexTile coordinates.
    /// <param name="q">HexTile coordinate q.</param>
    /// <param name="r">HexTile coordinate r.</param>
    /// </summary>
    public HexTile GetHexTile(int q, int r)
    {
        //Fix for negative indexes. Translates them to array indexes.
        return _hexGrid[r][q + r / 2];
    }

    /// <summary>
    /// This finds actual data in grid array.
    /// <param name="q">Array index.</param>
    /// <param name="r">Array index.</param>
    /// </summary>
    public HexTile GetHexTileDirect(int q, int r)
    {
        return _hexGrid[r][q];
    }

    /// <summary>
    /// This finds actual array indexes by reversing offset and scaling applied to HexCoord.
    /// </summary>
    /// <param name="coordinate">World Position of Tile.</param>
    public HexTile GetHexTile(Vector3 coordinate)
    {
        Vector2 noOffset = new Vector2(coordinate.x - _startPos.x, coordinate.z - _startPos.z) * _hexScaleFactor;
        Vector2 qrVector2 = HexCoord.VectorXYtoQR(noOffset);
        return GetHexTile(Mathf.RoundToInt(qrVector2.x), Mathf.RoundToInt(qrVector2.y));
    }

    /// <summary>
    /// This finds actual array indexes by reversing offset and scaling applied to HexCoord.
    /// </summary>
    /// <param name="coordinate">World Position of Tile.</param>
    public HexTile GetHexTile(Vector2 coordinate)
    {
        Vector2 noOffset = new Vector2(coordinate.x - _startPos.x, coordinate.y - _startPos.z) * _hexScaleFactor;
        Vector2 qrVector2 = HexCoord.VectorXYtoQR(noOffset);
        return GetHexTile(Mathf.RoundToInt(qrVector2.x), Mathf.RoundToInt(qrVector2.y));
    }

    /// <summary>
    /// Modified breadth first search algorithm that enumarates all hexes that are reachable by given range.
    /// </summary>
    /// <param name="centerHex">Start hex.</param>
    /// <param name="range">Range to search.</param>
    /// <param name="allowOccupiedAsLast">Allow occupied hexes if they are on the last node.</param>
    /// <returns>Enumarates and returns found HexTiles.</returns>
    public IEnumerable<HexTile> HexesInReachableRange(HexCoord centerHex, int range, bool allowOccupiedAsLast)
    {
        List<HexCoord> visited = new List<HexCoord> {centerHex};

        Queue<HexCoord> frontier = new Queue<HexCoord>();
        frontier.Enqueue(centerHex);

        int currentCost = 0;
        int elementsToDepthIncrease = 1;
        int nextElementsToDepthIncrease = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            HexTile hexTile = GetHexTile(current);
            yield return hexTile;
            foreach (HexCoord neighbor in current.Neighbors())
            {
                //Early exit if already checked this node
                if (visited.Contains(neighbor)) continue;
                //Make sure we are checking inside the board.
                if (!IsCordinateValid(neighbor)) continue;
                HexTile neighborTile = GetHexTile(neighbor);
                //Make sure it is passable. Allow occupied hexes if they are the last node.
                if (neighborTile.IsPassable
                    || (allowOccupiedAsLast && neighborTile.IsOccupied
                        && range - currentCost == GetHexTile(neighbor).MovementCost))
                {
                    //Only increasenextElementsToDepthIncrease if element is added to the queue.
                    nextElementsToDepthIncrease++;
                    frontier.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
            if (--elementsToDepthIncrease == 0)
            {
                currentCost += hexTile.MovementCost;
                if (currentCost > range) yield break;
                elementsToDepthIncrease = nextElementsToDepthIncrease;
                nextElementsToDepthIncrease = 0;
            }
        }
    }

    /// <summary>
    /// Finds Hexes in given radius from center.
    /// </summary>
    /// <param name="centerHex">Start hex.</param>
    /// <param name="range">Range to search.</param>
    /// <returns>Enumarates and returns found HexTiles.</returns>
    public IEnumerable<HexTile> HexesInRange(HexCoord centerHex, int range)
    {
        return HexesInRange(centerHex, 0, range);
    }

    /// <summary>
    /// Finds Hexes in given min and max radius from center.
    /// </summary>
    /// <param name="centerHex">Start hex.</param>
    /// <param name="minRange">Min. Range to search.</param>
    /// <param name="maxRange">Max. Range to search.</param>
    /// <returns>Enumarates and returns found HexTiles.</returns>
    public IEnumerable<HexTile> HexesInRange(HexCoord centerHex, int minRange, int maxRange)
    {
        for (int r = Mathf.Max(centerHex.r - maxRange, 0); r <= Mathf.Min(centerHex.r + maxRange, _hexGrid.Length - 1);
            r++)
        {
            int calculatedQ = (centerHex.q + r / 2);
            for (int q = Mathf.Max(calculatedQ - maxRange, 0);
                q <= Mathf.Min(calculatedQ + maxRange, _hexGrid[r].Length - 1); q++)
            {
                //if (!IsCordinateValid(q, r)) continue;
                HexTile tempHex = GetHexTileDirect(q, r);
                if (centerHex != tempHex.Coord)
                {
                    float distance = Mathf.Abs(HexCoord.Distance(centerHex, tempHex.Coord));
                    if (distance <= maxRange && distance >= minRange)
                        yield return tempHex;
                }
            }
        }
    }

    #endregion
}