using Godot;
using System;
using System.Collections.Generic;

public partial class game : Node2D
{
	public enum TileType
	{
		black = 0,
		stone = 1,
		wall = 2,
		floor = 3,
		stairs = 4,
		door = 5
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
				Map[x].Add(TileType.stone);
				_TileMap.SetCell(0, new Vector2I(x, y), (int)TileType.stone, new Vector2I(0, 0));
			}
		}
	}
}
