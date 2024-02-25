using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
		new (20 , 20),
		new (25 , 25),
		new (30 , 30),
		new (35 , 35),
		new (40 , 40),
	};
	public int[] LEVEL_ROOMS_COUNT = { 9, 13, 14, 16, 18 }; 
	public int[] LEVEL_ENEMIES_COUNT = { 5, 8, 12, 18, 24 }; 

	//==============================================================
	//CURRENT LEVEL
	//==============================================================
	public int current_level_number = 0;
	public Vector2 LevelSize = new Vector2();
	public Vector2 RoomSize = new Vector2();
	public List<Rect2> Rooms = new();
	public List<List<TileType>> Map = new();
	public List<Enemy> Enemies = new();

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
	private TileMap _visibilityMap;
	private Label _scoreLabel;
	private ColorRect _winScreen;
	private int TileLayer = 0;
    public PackedScene EnemyScene = ResourceLoader.Load<PackedScene>("res://Enemy.tscn");
    protected TileMap VisibilityMap => _visibilityMap ??= this.GetNode<TileMap>(new NodePath("VisibilityMap"));
	protected Label ScoreLabel => _scoreLabel ??= this.GetNode<Label>(new NodePath("UiLayer/Score"));


	public class Enemy
	{
		public Sprite2D scene { get; set; }
		public Vector2 TilePosition { get; set; }
		public int FullHp { get; set; }
		public int Hp {  get; set; }
		public bool IsDead => Hp <= 0;


		public Enemy(game game, int level, float x, float y)
		{
			FullHp = 1 + 2* level;
			Hp = FullHp;
			TilePosition = new Vector2(x, y);
			scene = game.EnemyScene.Instantiate<Sprite2D>();
			scene.Position = TilePosition * TILE_SIZE;
			game.AddChild(scene);
		}

		public void TakeDamage(int amount, game game)
		{
			if(IsDead) return;

			Hp = Math.Max(0, Hp - amount);
			var rect = scene.GetNode<ColorRect>("HealthBar");
			rect.SetSize(new Vector2(TILE_SIZE * ((float)Hp / FullHp), rect.Size.Y));

			game.Score += 10 * FullHp;
		}


		public void Remove()
		{
			scene.QueueFree();
		}
	}





    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		DisplayServer.WindowSetSize(new Vector2I(1280, 720));
		//DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
		_player = this.GetNode<Sprite2D>(new NodePath("Player"));
		_TileMap = this.GetNode<TileMap>(new NodePath("TileMap"));
		_winScreen = this.GetNode<ColorRect>("UiLayer/Win");
		BuildLevel();
		
        //CallDeferred(nameof(UpdateVisuals));
        //UpdateVisuals();
    }

    private bool visualsUpdated = false;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!visualsUpdated)
        {
            UpdateVisuals();
            visualsUpdated = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
		if (!@event.IsPressed())
			return;

		if (@event.IsAction("Left") )
		{
			TryMove(-1, 0);
		}
        else if (@event.IsAction("Right") )
        {
            TryMove(1, 0);
        }
        else if (@event.IsAction("Up"))
        {
            TryMove(0, -1);
        }
        else if (@event.IsAction("Down"))
        {
            TryMove(0, 1);
        }
    }

	

	private void _on_restart_pressed()
	{
        _winScreen.Visible = false;
		current_level_number = 0;
		Score = 0;
		BuildLevel();
        CallDeferred(nameof(UpdateVisuals));
    }


	private void TryMove(int dx, int dy)
	{
		var x = (int)PlayerTile.X + dx;
		var y = (int)PlayerTile.Y + dy;
		var tileType = TileType.Stone;

		if(x >= 0 && x <= LevelSize.X - 2 && y >= 0 && y <= LevelSize.Y - 2)
		{
			tileType = Map[x][y];
		}

		switch(tileType)
		{
			case TileType.Floor:

				var blocked = false;

				foreach(var enemy in Enemies)
				{
					if((int)enemy.TilePosition.X == x && (int)enemy.TilePosition.Y == y)
					{
						enemy.TakeDamage(1, this);
						if (enemy.IsDead)
						{
							enemy.Remove();
							Enemies.Remove(enemy);
						}
						blocked = true; 
						break;
					}
				}
				if (!blocked)
				{
					PlayerTile = new Vector2(x, y);
				}
                break;
            case TileType.Door:
				SetTile(x, y, TileType.Floor); break;
			case TileType.Stairs:
				current_level_number++;
				Score += 20;
				if (current_level_number < LEVEL_SIZES.Length)
				{
					BuildLevel();
                }
				else
				{
					Score += 1000;
					_winScreen.Visible = true;
				}
				break;
		}
        CallDeferred(nameof(UpdateVisuals));
		
    }



    private void BuildLevel()
	{
		Rooms.Clear();
		Map.Clear();
		_TileMap.Clear();
		VisibilityMap.Clear();

		foreach(var enemy in Enemies)
		{
			enemy.Remove();
		}

		LevelSize = LEVEL_SIZES[current_level_number];

		for (int x = 0; x < LevelSize.X; x++)
		{
			Map.Add(new());
			for (int y = 0; y < LevelSize.Y; y++)
			{
				Map[x].Add(TileType.Stone);
				_TileMap.SetCell(TileLayer, new Vector2I(x, y), (int)TileType.Stone, new Vector2I(0, 0));
				VisibilityMap.SetCell(TileLayer, new Vector2I(x, y), 0, new Vector2I(0, 0));
			}
		}

		var freeRegions = new List<Rect2>() { new Rect2(new Vector2(2, 2), LevelSize - new Vector2(4, 4)) };
		var numberOfRoom = LEVEL_ROOMS_COUNT[current_level_number];

		for (int i = 0; i < numberOfRoom; i++)
		{
			AddRoom(freeRegions);

			if (freeRegions.Count == 0)
			{
				break;
			}
		}
		ConnectRooms();

		//place player
		var startRoom = Rooms.First();
		var random = new Random();
		var playerX = startRoom.Position.X + 1 + random.Next() % ((int)(startRoom.Size.X - 2));
		var playerY = startRoom.Position.Y + 1 + random.Next() % ((int)(startRoom.Size.Y - 2));
		PlayerTile = new Vector2(playerX, playerY);

		//place enemies
		var numberOfEnemies = LEVEL_ENEMIES_COUNT[current_level_number];
		for (int i = 0;i < numberOfEnemies;i++)
		{
			var room = Rooms[1 + random.Next() % (Rooms.Count - 1)];
			var x = room.Position.X + 1 + random.Next() % ((int)(room.Size.X - 2));
			var y = room.Position.Y + 1 + random.Next() % ((int)(room.Size.Y - 2));

			var blocked = false;
			foreach(var enemy in Enemies)
			{
				if((int)enemy.TilePosition.X == x && (int)enemy.TilePosition.Y == y)
				{
					blocked = true; break;
				}
			}

			if (!blocked)
			{
				var enemy = new Enemy(this, random.Next() % 2, x, y);
				Enemies.Add(enemy);
			}
		}


		//place ladder
		var endRoom = Rooms.Last();
        var ladderX = endRoom.Position.X + 1 + random.Next() % ((int)(endRoom.Size.X - 2));
        var ladderY = endRoom.Position.Y + 1 + random.Next() % ((int)(endRoom.Size.Y - 2));
		SetTile((int)ladderX, (int)ladderY, TileType.Stairs);
		var levelLabel = GetNode<Label>("UiLayer/Level");
		levelLabel.Text = $"Level: {current_level_number + 1}";

    }

	private void UpdateVisuals()
	{
		_player.Position = PlayerTile * TILE_SIZE;

		try
		{
			var playerCenter = TileToPixelCenter(PlayerTile.X, PlayerTile.Y);
			var spaceState = _TileMap.GetWorld2D().DirectSpaceState;

            for (int x = 0; x < LevelSize.X; x++)
			{
				for (int y = 0; y < LevelSize.Y; y++)
				{
                    if ( VisibilityMap.GetCellSourceId(0, new Vector2I(x, y)) == 0)
					{
						var xDir = (x < PlayerTile.X) ? 1 : -1;
						var yDir = (y < PlayerTile.Y) ? 1 : -1;
						var testPoint = TileToPixelCenter(x, y) + new Vector2(xDir, yDir) * (TILE_SIZE / 2);

                        var parameters = PhysicsRayQueryParameters2D.Create(playerCenter, testPoint);
                        var occlusion = spaceState.IntersectRay(parameters);

                        if (occlusion == null ||
							occlusion.Count == 0 ||
							(occlusion["position"].AsVector2() - testPoint).Length() < 1
							)
						{
							VisibilityMap.SetCell(TileLayer, new Vector2I(x, y), -1);
						}
					}
				}
			}

			ScoreLabel.Text = $"Score: {Score}";
		}
		catch (Exception ex)
		{
            Console.WriteLine(ex);
        }

	}


	private Vector2 TileToPixelCenter(float x, float y)
	{
		return new Vector2((x + 0.5f) * TILE_SIZE, (y + 0.5f) * TILE_SIZE);
	}


	private void ConnectRooms()
	{
		var stoneGraph = new AStar2D();
		var pointId = 0l;

		//build AStart graph for stones tiles
		for (int x = 0; x < LevelSize.X; x++)
		{
			for (int y = 0; y < LevelSize.Y; y++)
			{
				if (Map[x][y] == TileType.Stone)
				{
					var weight = 0;
					stoneGraph.AddPoint(pointId, new Vector2I(x, y), weight);

					//connect to left if also stone
					if(x > 0 && Map[x-1][y] == TileType.Stone)
					{
						var leftPoint = stoneGraph.GetClosestPoint(new Vector2I(x - 1, y));
						stoneGraph.ConnectPoints(pointId, leftPoint);
					}

					//connect to above if also stone
					if(y > 0 && Map[x][y - 1] == TileType.Stone)
					{
						var abovePoint = stoneGraph.GetClosestPoint(new Vector2I(x, y - 1));
                        stoneGraph.ConnectPoints(pointId, abovePoint);
                    }
					pointId++;
				}
			}
		}

		//Build an AStar graph for rooms connection
		var roomGraph = new AStar2D();
		pointId = 0;
		foreach(var room in Rooms)
		{
			roomGraph.AddPoint(pointId, room.GetCenter(), 0);
			pointId++;
		}

		//add random connection untill everything is connected
		while (!IsEverythingConnected(roomGraph))
		{
			AddRandomConnection(stoneGraph, roomGraph);
		}
	}
	private bool IsEverythingConnected(AStar2D roomGraph)  
	{
		var points = roomGraph.GetPointIds();
        var startPoint =  points.First();

		foreach(var toPoint in points)
		{
			if(toPoint == startPoint)
			{
				continue;
			}

			var path = roomGraph.GetPointPath(startPoint, toPoint);
			if(path == null || path.Length == 0)
			{
				return false;
			}
		}

		return true;
	}
	private void AddRandomConnection(AStar2D stoneGraph, AStar2D roomGraph)
	{
		var startRoomId = GetLeastConnectedPoint(roomGraph);
		var endRoomId = GetNearestUnconectedPoint(roomGraph, startRoomId);

		//pick door location
		var startPosition = PickRandomDoorLocation(Rooms[(int)startRoomId]);
		var endPosition = PickRandomDoorLocation(Rooms[(int)endRoomId]);

		//find path to connect doors to each others
		var closestStartPoint = stoneGraph.GetClosestPoint(startPosition);
		var closestEndPoint = stoneGraph.GetClosestPoint(endPosition);

		var path = stoneGraph.GetPointPath(closestStartPoint, closestEndPoint);
		
		SetTile((int)startPosition.X, (int)startPosition.Y, TileType.Door);
		SetTile((int)endPosition.X, (int)endPosition.Y, TileType.Door);

		foreach(var position in path)
		{
			SetTile((int)position.X, (int)position.Y, TileType.Floor);
		}

		roomGraph.ConnectPoints(startRoomId, endRoomId, true);
	}
	private long GetLeastConnectedPoint(AStar2D roomGraph)
	{
		var pointsIds = roomGraph.GetPointIds();
		var tiedForLeast = new List<long>();
		var least = 1_000_000l;

		foreach(var pointId in pointsIds)
		{
			var count = roomGraph.GetPointConnections(pointId).Count();

			if(count < least)
			{
				least = count;
				tiedForLeast = new List<long>() { pointId };
            }
			else if(count == least)
			{
				tiedForLeast.Add(pointId);
			}
		}

		var rand = new Random();
		return tiedForLeast[rand.Next() % tiedForLeast.Count];
	}
	private long GetNearestUnconectedPoint(AStar2D roomGraph, long targetPoint)
	{
		var targetPosition = roomGraph.GetPointPosition(targetPoint);
		var pointsIds = roomGraph.GetPointIds();

		var nearest = 1_000_000l;
		var tiedForNearest = new List<long>();

		foreach(var pointId in pointsIds)
		{
			if(pointId == targetPoint)
			{
				continue;
			}

			var path = roomGraph.GetPointPath(pointId, targetPoint);
			if(path != null && path.Length != 0)
			{
				continue;
			}

			var dist = (roomGraph.GetPointPosition(pointId) - targetPosition).Length();
			if(dist < nearest)
			{
				nearest = (long)dist;
				tiedForNearest = new List<long>() { pointId };
			}
			else if ((long)dist == nearest)
			{
				tiedForNearest.Add(pointId);
			}
		}
        var rand = new Random();
        return tiedForNearest[rand.Next() % tiedForNearest.Count];
    }
	private Vector2 PickRandomDoorLocation(Rect2 room)
	{
		var options = new List<Vector2>();

		//Top and bottom walls
		for (int x = (int)room.Position.X + 1; x < room.End.X - 2; x++)
		{
			options.Add(new (x, room.Position.Y));
			options.Add(new (x, room.End.Y - 1));
		}

		//left and right walls
		for (int y = (int)room.Position.Y + 1; y < room.End.Y - 2; y++)
		{
			options.Add(new (room.Position.X, y));
			options.Add(new (room.End.X - 1, y));
		}

		var rand = new Random();
		return options[rand.Next() % options.Count];
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
        _TileMap.SetCell(TileLayer, new Vector2I(x, y), (int)tileType, new Vector2I(0, 0));
    }
}
