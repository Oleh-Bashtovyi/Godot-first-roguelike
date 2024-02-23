using Godot;
using System;
using System.Collections.Generic;

public partial class game : Node2D
{
	public enum TileType
	{
		Black = 0,
		Stone = 1,
		Wall = 2,
		Floor = 3,
		Stairs = 4,
		Door = 5
	}

    //==============================================================
    //CONSTANTS
    //==============================================================
    public const int TILE_SIZE = 32;
	public const int MIN_ROOM_DIMENSION = 5;
	public const int MAX_ROOM_DIMENSION = 8;

	public Vector2[] LEVEL_SIZES = 
	{
		new (30 , 30),
		new (35 , 35),
		new (40 , 40),
		new (45 , 45),
		new (50 , 50),
	};
	public int[] LEVEL_ROOMS_COUNT = { 5, 7, 9, 12, 15 }; 

	//==============================================================
	//CURRENT LEVEL
	//==============================================================
	public int current_level_number = 0;
	public Vector2 RoomSize = new Vector2();
	public List<Rect2> Rooms = new();
	public List<List<TileType>> Map = new();

    //==============================================================
    //GAME STATE
    //==============================================================
	public Vector2 PlayerTile = new();
	public int Score = 0;

	//==============================================================
	//INTERNAL
	//==============================================================
	private Sprite2D _player;
	private TileMap _TileMap;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		DisplayServer.WindowSetSize(new Vector2I(1280, 720));
		_player = this.GetNode<Sprite2D>(new NodePath("Player"));
		_TileMap = this.GetNode<TileMap>(new NodePath("TileMap"));
		BuildLevel();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}



	private void BuildLevel()
	{
		Rooms.Clear();
		Map.Clear();
		_TileMap.Clear();

		var levelSize = LEVEL_SIZES[current_level_number];

		for (int x = 0; x < levelSize.X; x++)
		{
			Map.Add(new());
			for (int y = 0; y < levelSize.Y; y++)
			{
				//SetTile(x, y, TileType.Stone);
				Map[x].Add(TileType.Stone);
				_TileMap.SetCell(0, new Vector2I(x, y), (int)TileType.Stone, new Vector2I(0, 0));
			}
		}

		var freeRegions = new List<Rect2>() { new Rect2(new Vector2(2, 2), levelSize - new Vector2(4, 4)) };
		var numberOfRoom = LEVEL_ROOMS_COUNT[current_level_number];

		for (int i = 0; i < numberOfRoom; i++)
		{
			AddRoom(freeRegions);

			if (freeRegions.Count == 0)
			{
				break;
			}
		}

	}


	private void AddRoom(List<Rect2> freeRegions)
	{
		var rand = new Random();
		var region = freeRegions[rand.Next() % freeRegions.Count];

		var sizeX = MIN_ROOM_DIMENSION;
		if( region.Size.X > MIN_ROOM_DIMENSION )
		{
			sizeX += rand.Next() % ((int)(region.Size.X - MIN_ROOM_DIMENSION));
		}

        var sizeY = MIN_ROOM_DIMENSION;
        if (region.Size.Y > MIN_ROOM_DIMENSION)
        {
            sizeY += rand.Next() % ((int)(region.Size.Y - MIN_ROOM_DIMENSION));
        }

		sizeX = Math.Min(sizeX, MAX_ROOM_DIMENSION);
		sizeY = Math.Min(sizeY, MAX_ROOM_DIMENSION);

		var startX = region.Position.X;
		if(region.Size.X > sizeX )
		{
			startX += rand.Next() % ((int)(region.Size.X - sizeX));
		}

        var startY = region.Position.Y;
        if (region.Size.Y > sizeY)
        {
            startY += rand.Next() % ((int)(region.Size.Y - sizeY));
        }

		var room = new Rect2(startX, startY, sizeX, sizeY);
		Rooms.Add(room);

		for (int x = (int)startX; x < startX + sizeX; x++)
		{
			SetTile(x, (int)startY, TileType.Wall);
			SetTile(x, (int)(startY + sizeY - 1), TileType.Wall);
		}

		for (int y = (int)(startY + 1); y < startY + sizeY - 1; y++)
		{
			SetTile((int)startX, y, TileType.Wall);
			SetTile((int)(startX + sizeX - 1), y, TileType.Wall);

			for (int x = (int)(startX + 1); x < (startX + sizeX - 1); x++)
			{
				SetTile(x, y, TileType.Floor);
			}
		}

		CutRegions(freeRegions, room);
    }



	private void CutRegions(List<Rect2> freeRegions, Rect2 regionToRemove)
	{
		var removalQueue = new List<Rect2>();
		var additionQueue = new List<Rect2>();

		foreach (var region in freeRegions)
		{
			if (region.Intersects(regionToRemove))
			{
				removalQueue.Add(region);

				var leftover_left = regionToRemove.Position.X - region.Position.X - 1;
				var leftover_right = region.End.X - regionToRemove.End.X - 1;
				var leftover_above = regionToRemove.Position.Y - region.Position.Y - 1;
				var leftover_below = region.End.Y - regionToRemove.End.Y - 1;

				if(leftover_left >= MIN_ROOM_DIMENSION)
				{
					additionQueue.Add(new Rect2(region.Position, new Vector2(leftover_left, region.Size.Y)));
				}
				if(leftover_right >= MIN_ROOM_DIMENSION)
				{
					additionQueue.Add(new Rect2(new Vector2(regionToRemove.End.X + 1, region.Position.Y), 
													 new Vector2(leftover_right, region.Size.Y)));
				}
                if (leftover_above >= MIN_ROOM_DIMENSION)
                {
                    additionQueue.Add(new Rect2(region.Position, new Vector2(region.Size.X, leftover_above)));
                }
                if (leftover_below >= MIN_ROOM_DIMENSION)
                {
                    additionQueue.Add(new Rect2(new Vector2(region.Position.X, regionToRemove.End.Y + 1),
                                                     new Vector2(region.Size.X, leftover_below)));
                }

            }
		}
		foreach(var item in removalQueue)
		{
			freeRegions.Remove(item);
		}

		foreach(var region in additionQueue)
		{
			freeRegions.Add(region);
		}
	}


	private void SetTile(int x, int y, TileType tileType)
	{
		Map[x][y] = tileType;
        _TileMap.SetCell(0, new Vector2I(x, y), (int)tileType, new Vector2I(0, 0));
    }
}
