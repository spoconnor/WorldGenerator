﻿using System;
using System.Collections.Concurrent;
using System.Xml;
using Sean.Shared;

namespace Sean.WorldGenerator
{
	public enum GameItemType
	{
		BlockItem,
		GameItem,
		Projectile
	}

	/// <summary>
	/// Game items that are dynamic and can move and/or decay.
	/// Stored at the chunk and world levels.
	/// </summary>
	public abstract class GameItemDynamic : GameObject
	{
		#region Constructors
		public GameItemDynamic(ref Coords coords, GameItemType type, bool allowBounce, Vector3? velocity = null, int id = -1) : base(ref coords, id)
		{
			Type = type;
			AllowBounce = allowBounce;
			if (velocity.HasValue) Velocity = velocity.Value;
			IsMoving = true;
			if (!World.GameItems.ContainsKey(Id)) World.GameItems.TryAdd(Id, this);
            var chunk = World.WorldMap.Chunk(coords);
			if (!chunk.GameItems.ContainsKey(Id)) chunk.GameItems.TryAdd(Id, this);
		}

		public GameItemDynamic(XmlNode xmlNode) : base(xmlNode)
		{
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			Type = (GameItemType)int.Parse(xmlNode.Attributes["T"].Value);
			AllowBounce = true;
			Velocity = new Vector3(float.Parse(xmlNode.Attributes["VX"].Value), float.Parse(xmlNode.Attributes["VY"].Value), float.Parse(xmlNode.Attributes["VZ"].Value));
			IsMoving = true;
			try
			{
                var chunk = World.WorldMap.Chunk(Coords);
				if (!chunk.GameItems.ContainsKey(Id)) chunk.GameItems.TryAdd(Id, this);
			}
			catch (Exception)
			{
				throw new Exception(string.Format("Tried to load GameItem outside the world. Id: {0} {1}", Id, Coords));
			}
			if (!World.GameItems.ContainsKey(Id)) World.GameItems.TryAdd(Id, this);
		}
		#endregion

		#region Events
        public delegate void BounceEventHandler(FrameEventArgs e);
		public event BounceEventHandler Bounce;

        public delegate void StopEventHandler(FrameEventArgs e);
		public event StopEventHandler Stop;

        public delegate void DecayEventHandler(FrameEventArgs e);
		public event DecayEventHandler Decay;
		#endregion

		#region Properties
		public virtual GameItemType Type { get; private set; }
		/// <summary>Game items bounce by default when running into anything that causes collision. Is not saved to the xml file because so far projectiles are the only items that might not bounce and they arent saved.</summary>
		public bool AllowBounce { get; set; }

		private bool _isMoving;
		public bool IsMoving
		{
			get { return _isMoving; }
			set
			{
				_isMoving = value;
				if (!_isMoving) Velocity = Vector3.Zero;
				LastUpdate = DateTime.Now;
			}
		}

		public DateTime LastUpdate;
		//private bool _queuedForDecay;
		public Vector3 Velocity = new Vector3((float)Settings.Random.NextDouble(), 5f, (float)Settings.Random.NextDouble());
		/// <summary>
		/// Items decay from the world when the item has not been updated for longer than the decay seconds.
		/// The last update time is not saved, therefore when a world is loaded the update time of each item is reset which also results in the decay logic being reset.
		/// Items that require a decay time different then the default can override this property.
		/// </summary>
		public virtual int DecaySeconds { get { return 900; } } //15min
		
		public override string XmlElementName
		{
			get { return "GI"; }
		}
		#endregion

		#region Operations
		public override XmlNode GetXml(XmlDocument xmlDocument)
		{
			var xmlNode = base.GetXml(xmlDocument);
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("T")).Value = ((int)Type).ToString();
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("VX")).Value = Velocity.X.ToString("0.##"); //this format uses the smallest number of chars possible to represent the coords to a precision of 2
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("VY")).Value = Velocity.Y.ToString("0.##"); //this format uses the smallest number of chars possible to represent the coords to a precision of 2
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("VZ")).Value = Velocity.Z.ToString("0.##"); //this format uses the smallest number of chars possible to represent the coords to a precision of 2
			return xmlNode;
		}

		public void Update(FrameEventArgs e)
		{
			//if (!_queuedForDecay && (Config.IsSinglePlayer || Config.IsServer || Type == GameItemType.Projectile) && !IsMoving && (DateTime.Now - LastUpdate).TotalSeconds >= DecaySeconds)
			//{
			//	_queuedForDecay = true;
			//	DecayQueue.Enqueue(this);
			//}

			if (!IsMoving) return;

			var bounced = false;
			var proposedCoords = Coords;
			if (!Equals(Velocity.X, 0f))
			{
				proposedCoords.Xf += Velocity.X * (float)e.Time;
                if (!World.IsValidItemLocation(proposedCoords) || (World.IsValidBlockLocation(proposedCoords) && World.GetBlock(ref proposedCoords).IsSolid))
				{
					//Bounce
					bounced = true;
					proposedCoords.Xf = Coords.Xf;
					Velocity.X = -Velocity.X / 2f;
					Velocity.Z /= 2f;
				}
			}

			if (!Equals(Velocity.Z, 0f))
			{
				proposedCoords.Zf += Velocity.Z * (float)e.Time;
                if (!World.IsValidItemLocation(proposedCoords) || (World.IsValidBlockLocation(proposedCoords) && World.GetBlock(ref proposedCoords).IsSolid))
				{
					//Bounce
					bounced = true;
					proposedCoords.Zf = Coords.Zf;
					Velocity.X /= 2f;
					Velocity.Z = -Velocity.Z / 2f;
				}
			}

			Velocity.Y += Constants.GRAVITY * (float)e.Time;
			proposedCoords.Yf = proposedCoords.Yf + Velocity.Y * (float)e.Time;
            if (!World.IsValidItemLocation(proposedCoords) || (World.IsValidBlockLocation(proposedCoords) && World.GetBlock(ref proposedCoords).IsSolid))
			{
				var currentVelocityY = Velocity.Y + Constants.GRAVITY * (float)e.Time;
				if (Velocity.LengthFast > 3f)
				{
					//Bounce
					bounced = true;
					proposedCoords.Yf = Coords.Yf;
					Velocity.X /= 2;
					Velocity.Y = -currentVelocityY / 2f;
					Velocity.Z /= 2;
				}
				else if (currentVelocityY >= 0)
				{
					//Hit ceiling, begin fall
					proposedCoords.Yf = Coords.Yf;
					Velocity.X /= 2;
					Velocity.Y = 0;
					Velocity.Z /= 2;
				}
				else
				{
					//Hit ground, stop
					do
					{
						proposedCoords.Yf++;
                    } while (!World.IsValidItemLocation(proposedCoords) || (World.IsValidBlockLocation(proposedCoords) && World.GetBlock(ref proposedCoords).IsSolid));
					proposedCoords.Yf = (float)Math.Floor(proposedCoords.Yf) + Constants.ITEM_HOVER_DIST;
					IsMoving = false;
					if (Stop != null) Stop(e);
				}
			}
			if (new ChunkCoords(ref Coords) != new ChunkCoords(ref proposedCoords))
			{
				//Moving to a new chunk
                var oldChunk = World.WorldMap.Chunk(Coords);
				GameItemDynamic remove;
				oldChunk.GameItems.TryRemove(Id, out remove);

                var newChunk = World.WorldMap.Chunk(proposedCoords);
				if (!newChunk.GameItems.ContainsKey(Id)) newChunk.GameItems.TryAdd(Id, this);
			}
			Coords = proposedCoords;
			LastUpdate = DateTime.Now;

			//if (bounced && Bounce != null) Bounce(e); //old
			if (bounced)
			{
				if (AllowBounce)
				{
					if (Bounce != null) Bounce(e);
				}
				else
				{
					IsMoving = false;
					if (Stop != null) Stop(e);
				}
			}
		}
		#endregion

		#region Static (should maybe move to a static Lib class)
		private static readonly ConcurrentQueue<GameItemDynamic> DecayQueue = new ConcurrentQueue<GameItemDynamic>();
		public static void UpdateAll(FrameEventArgs e)
		{
			foreach (var gameItem in World.GameItems.Values)
			{
				gameItem.Update(e);
			}
			
			GameItemDynamic decayItem;
			while (DecayQueue.TryDequeue(out decayItem))
			{
				switch (decayItem.Type)
				{
					case GameItemType.BlockItem:
                        // TODO
						//new GameActions.RemoveBlockItem(decayItem.Id, true).Receive();
						break;
					case GameItemType.Projectile:
                    var chunk = World.WorldMap.Chunk(decayItem.Coords);
						GameItemDynamic remove;
						chunk.GameItems.TryRemove(decayItem.Id, out remove);
						World.GameItems.TryRemove(decayItem.Id, out remove);
						break;
				}
				if (decayItem.Decay != null) decayItem.Decay(e);
			}
		}
		#endregion
	}
}