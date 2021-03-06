﻿using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Sean.Shared;
using OpenTK.Input;
using System.IO;

namespace OpenTkClient
{
	class GameRenderer : GameWindow
	{
		TextRenderer renderer;
		MouseState mouseState;
		float trimHeight = Global.CHUNK_HEIGHT;
		Font serif = new Font(FontFamily.GenericSerif, 24);
		Font sans = new Font(FontFamily.GenericSansSerif, 24);
		Font mono = new Font(FontFamily.GenericMonospace, 24);
		float angle;
		int[] textures = new int[255];
		int[] largeTextures = new int[255];
        int boxListIndex;
        int boxListLargeIndex;
		float mousePosX, mousePosY;
        Position selectedBlock = new Position(0,0,0);
        ChunkCoords selectedChunk = new ChunkCoords(0, 0);
        int selectedChunkHeight = 0;

        const int BlockTypeCursor = 51; // TODO

        public GameRenderer()
			: base(800, 600)
		{
			this.KeyPress += OnKeyPress;
			this.MouseWheel += OnMouseWheel;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
        }


        protected override void OnLoad(EventArgs e)
		{
            GL.ClearColor(Color.CornflowerBlue);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Disable(EnableCap.DepthTest);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);

			textures[(int)BlockType.Unknown] = LoadTexture("block.png");
			textures[(int)BlockType.Rock] = LoadTexture("rock.png");
			textures[(int)BlockType.Grass] = LoadTexture("grass.png");
			textures[(int)BlockType.Dirt] = LoadTexture("grass.png");
			textures[(int)BlockType.Tree] = LoadTexture("tree.png");
			textures[(int)BlockType.Leaves] = LoadTexture("leaves.png");
            textures[(int)BlockType.Ocean] = LoadTexture("water.png");
            textures[(int)BlockType.Water] = LoadTexture("water.png");
            textures[(int)BlockType.Water1] = LoadTexture("water1.png");
            textures[(int)BlockType.Water2] = LoadTexture("water2.png");
            textures[(int)BlockType.Water3] = LoadTexture("water3.png");
            textures[(int)BlockType.Water4] = LoadTexture("water4.png");
            textures[(int)BlockType.Water5] = LoadTexture("water5.png");
            textures[(int)BlockType.Water6] = LoadTexture("water6.png");
            textures[(int)BlockType.Water7] = LoadTexture("water7.png");
            textures[(int)BlockType.WaterSource] = LoadTexture("water.png");
			textures[(int)BlockType.Placeholder1] = LoadTexture("character.png");

            textures[(int)BlockType.GrassSlopeN] = LoadTexture("grass_slope_n.png");
            textures[(int)BlockType.GrassSlopeS] = LoadTexture("grass_slope_s.png");
            textures[(int)BlockType.GrassSlopeE] = LoadTexture("grass_slope_e.png");
            textures[(int)BlockType.GrassSlopeW] = LoadTexture("grass_slope_w.png");
            textures[(int)BlockType.GrassSlopeNW] = LoadTexture("grass_slope_nw.png");
            textures[(int)BlockType.GrassSlopeNE] = LoadTexture("grass_slope_ne.png");
            textures[(int)BlockType.GrassSlopeSE] = LoadTexture("grass_slope_se.png");
            textures[(int)BlockType.GrassSlopeSW] = LoadTexture("grass_slope_sw.png");
            textures[(int)BlockType.GrassSlopeNEW] = LoadTexture("grass_slope_new.png");
            textures[(int)BlockType.GrassSlopeNES] = LoadTexture("grass_slope_nes.png");
            textures[(int)BlockType.GrassSlopeESW] = LoadTexture("grass_slope_esw.png");
            textures[(int)BlockType.GrassSlopeNWS] = LoadTexture("grass_slope_nws.png");

			textures[(int)BlockTypeCursor] = LoadTexture("cursor.png");

            if (Global.CHUNK_SIZE == 32)
            {
                largeTextures[(int)BlockType.Rock] = LoadTexture("rock_32.png");
                largeTextures[(int)BlockType.Grass] = LoadTexture("grass_32.png");
                largeTextures[(int)BlockType.Dirt] = LoadTexture("grass_32.png");
                largeTextures[(int)BlockType.Ocean] = LoadTexture("water_32.png");
                largeTextures[(int)BlockType.Water1] = LoadTexture("water_32.png");
                largeTextures[(int)BlockTypeCursor] = LoadTexture("cursor_32.png");
            }
            else if (Global.CHUNK_SIZE == 16)
            {
                largeTextures[(int)BlockType.Rock] = LoadTexture("rock_16.png");
                largeTextures[(int)BlockType.Grass] = LoadTexture("grass_16.png");
                largeTextures[(int)BlockType.Dirt] = LoadTexture("grass_16.png");
                largeTextures[(int)BlockType.Ocean] = LoadTexture("water_16.png");
                largeTextures[(int)BlockType.Water1] = LoadTexture("water_16.png");
                largeTextures[(int)BlockTypeCursor] = LoadTexture("cursor_16.png");
            }

            renderer = new TextRenderer(Width, Height);
			PointF position = PointF.Empty;

			renderer.Clear(Color.Transparent);
			renderer.DrawString("The quick brown fox jumps over the lazy dog", serif, Brushes.White, position);
			position.Y += serif.Height;
			renderer.DrawString("The quick brown fox jumps over the lazy dog", sans, Brushes.White, position);
			position.Y += sans.Height;
			renderer.DrawString("The quick brown fox jumps over the lazy dog", mono, Brushes.White, position);
			position.Y += mono.Height;

            boxListIndex = CompileBox();
            boxListLargeIndex = CompileLargeBox ();
        }

		private int LoadTexture(string filename)
		{
			int texture;
			Bitmap bitmap = new Bitmap(Path.Combine("Resources",filename));

			GL.GenTextures(1, out texture);
			GL.BindTexture(TextureTarget.Texture2D, texture);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                              ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                          OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);
            bitmap.Dispose();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return texture;
		}

		protected override void OnUnload(EventArgs e)
		{
			renderer.Dispose();
		}

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(ClientRectangle);
		}

        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            trimHeight = e.ValuePrecise + Global.CHUNK_HEIGHT;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine($"Mouse click. {selectedBlock} - {selectedChunk} {selectedChunkHeight}");

            //GL.MatrixMode(MatrixMode.Projection);        // Select the Projection matrix for operation
            //GL.LoadIdentity();                           // Reset Projection matrix
            //GL.Ortho(0, this.Width * Global.Scale, 0, this.Height * Global.Scale, -1.0, 1.0);             // Set clipping area's left, right, bottom, top
            //GL.MatrixMode(MatrixMode.Modelview);         // Select the ModelView for operation
            //GL.LoadIdentity();                           // Reset the Model View Matrix

            //float pixels = 0.0f;
            //GL.ReadPixels<float>(mousePosX, mousePosY, 1, 1, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, ref pixels);
            //Console.WriteLine(pixels);

            //Console.WriteLine($"{mousePosX},{mousePosY}");
        }

        private void OnMouseMove(object sender, MouseMoveEventArgs e)
        {
            mousePosX = e.X * Global.Scale;
            mousePosY = (this.Height - e.Y) * Global.Scale;
        }

        void OnKeyPress (object sender, KeyPressEventArgs e)
		{
            if (e.KeyChar == '1') {
                Global.Direction = Facing.North;
            } else if (e.KeyChar == '2') {
                Global.Direction = Facing.East;
            } else if (e.KeyChar == '3') {
                Global.Direction = Facing.South;
            } else if (e.KeyChar == '4') {
                Global.Direction = Facing.West;
            }
		}
			
		protected override void OnUpdateFrame(FrameEventArgs e)
		{			
			if (Keyboard[OpenTK.Input.Key.Escape])
			{
				this.Exit();
			}

			KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Key.W))
            {
                Global.LookingAt = new Position(Global.LookingAt.X - (int)Math.Max(1, Global.Scale), Global.LookingAt.Y, Global.LookingAt.Z);
            }
            else if (keyState.IsKeyDown(Key.A))
            {
                Global.LookingAt = new Position(Global.LookingAt.X + (int)Math.Max(1, Global.Scale), Global.LookingAt.Y, Global.LookingAt.Z);
            }
            else if (keyState.IsKeyDown(Key.S))
            {
                Global.LookingAt = new Position(Global.LookingAt.X, Global.LookingAt.Y, Global.LookingAt.Z + (int)Math.Max(1, Global.Scale));
            }
            else if (keyState.IsKeyDown(Key.Q))
            {
                Global.LookingAt = new Position(Global.LookingAt.X, Global.LookingAt.Y, Global.LookingAt.Z - (int)Math.Max(1, Global.Scale));
            }
            else if (keyState.IsKeyDown(Key.E))
            {
                Global.LookingAt = new Position(Global.LookingAt.X, Global.LookingAt.Y+1, Global.LookingAt.Z);
            }
            else if (keyState.IsKeyDown(Key.D))
            {
                Global.LookingAt = new Position(Global.LookingAt.X, Global.LookingAt.Y-1, Global.LookingAt.Z);

            } else if (keyState.IsKeyDown(Key.PageUp)) {
                Global.Scale = Global.Scale - Global.Scale / 10;
            } else if (keyState.IsKeyDown(Key.PageDown)) {
                Global.Scale = Global.Scale + Global.Scale / 10;
            }

//			if (keyState.IsKeyDown (Key.Number1)) {
//				Global.Direction = Facing.North;
//			} else if (keyState.IsKeyDown (Key.Number2)) {
//				Global.Direction = Facing.East;
//			} else if (keyState.IsKeyDown (Key.Number3)) {
//				Global.Direction = Facing.South;
//			} else if (keyState.IsKeyDown (Key.Number4)) {
//				Global.Direction = Facing.West;
//			}
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
            //RenderGui();
            GL.MatrixMode(MatrixMode.Projection);        // Select the Projection matrix for operation
            GL.LoadIdentity();                           // Reset Projection matrix
			GL.Ortho(0, this.Width * Global.Scale, 0, this.Height * Global.Scale, -1.0, 1.0);             // Set clipping area's left, right, bottom, top
            GL.MatrixMode(MatrixMode.Modelview);         // Select the ModelView for operation
            GL.LoadIdentity();                           // Reset the Model View Matrix

            GL.Clear(ClearBufferMask.ColorBufferBit);// | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.DepthTest);

            GL.PushMatrix();
            // Render World Map
            var avgHeight = 0;
            var count = 0;
            foreach (var poly in MapManager.GetWorldMapBlocks(Global.Direction))
            {
                var scrPos = WorldToScreen (poly.Item1.X, poly.Item1.Y, poly.Item1.Z, largeBlock:true);
                RenderLargeBlock((float)e.Time,poly.Item2, scrPos.Item1, scrPos.Item2, scrPos.Item3,poly.Item1);
                if (Math.Abs(poly.Item1.Z - Global.LookingAt.Z) <= 100 && Math.Abs(poly.Item1.X - Global.LookingAt.X) <= 100)
                {
                    avgHeight += poly.Item1.Y;
                    count++;
                }
                //scrPos = WorldToScreen(poly[1].X, poly[1].Y, poly[1].Z);
                //RenderLargeBlock((float)e.Time,(Block.BlockType)TextureGrass,scrPos.Item1, scrPos.Item2, scrPos.Item3,poly[1]);
                //scrPos = WorldToScreen(poly[2].X, poly[2].Y, poly[2].Z);
                //RenderLargeBlock((float)e.Time,(Block.BlockType)TextureGrass,scrPos.Item1, scrPos.Item2, scrPos.Item3,poly[2]);
                //scrPos = WorldToScreen(poly[3].X, poly[3].Y, poly[3].Z);
                //RenderLargeBlock((float)e.Time,(Block.BlockType)TextureGrass,scrPos.Item1, scrPos.Item2, scrPos.Item3,poly[3]);
            }
            if (count > 0)
                Global.LookingAt = new Position(Global.LookingAt.X, avgHeight / count, Global.LookingAt.Z);
            GL.PopMatrix ();

            // Render Local Chunks
            int drawCount = 0;
			int cullCount = 0;
            foreach (var blockInfo in MapManager.GetBlocks(Global.Direction))
            {
                var pos = blockInfo.Item1;
                var blockType = blockInfo.Item2;
                var scrPos = WorldToScreen (pos.X, pos.Y, pos.Z);
				if (scrPos.Item1 < 0 || scrPos.Item1 > (this.Width * Global.Scale) 
					|| scrPos.Item2 < 0 || scrPos.Item2 > (this.Height * Global.Scale)) {
					//Console.WriteLine($"{x1},{y1},{z1}=>{x2},{y2},{z2}");
					cullCount++;
				}
				else
				{
					if (pos.Y < trimHeight) {
						drawCount++;
						RenderBlock ((float)e.Time, blockType, scrPos.Item1, scrPos.Item2, scrPos.Item3, pos);
					}
				}
            }

            foreach (var character in CharacterManager.GetCharacters(Global.Direction))
            {
				drawCount++;
                var scrPos = WorldToScreen(character.Item1.X, character.Item1.Y, character.Item1.Z);
				RenderBlock((float)e.Time, BlockType.Placeholder1, scrPos.Item1, scrPos.Item2, scrPos.Item3, character.Item1); // TODO - sprite block type
                scrPos = WorldToScreen(character.Item1.X, character.Item1.Y + 1, character.Item1.Z);
				RenderBlock((float)e.Time, BlockType.Placeholder1, scrPos.Item1, scrPos.Item2, scrPos.Item3, character.Item1); // TODO - sprite block type
            }
            //Console.WriteLine ($"DrawCount:{drawCount}, Culled:{cullCount}");

            SwapBuffers();
		}

        private Tuple<float,float,float> WorldToScreen(int x, int y, int z, bool largeBlock=false)
        {
            int midWidth = (int)(this.Width * Global.Scale / 2);
            int midHeight = (int)(this.Height * Global.Scale / 2);
            const int sprXOffset = 16;
            const int sprYOffset = 8;
            const int sprHeight = 16;
            int blockOffset = largeBlock ? 0 : 128 - sprHeight;

            float x1 = x - Global.LookingAt.X;
            float y1 = y - Global.LookingAt.Y;
            float z1 = z - Global.LookingAt.Z;

            //if (y1 > 1 || x1 > 5 || z1 > 5) continue;

            float x2 = 0.0f, y2 = 0.0f, z2 = 0.0f;//(y1 + (x1 + z1) * 128.0f) / (64.0f * 128.0f);
            switch (Global.Direction)
            {
                case Facing.North:
                    x2 = midWidth + (z1 - x1) * sprXOffset;
                    y2 = midHeight + (y1 * sprHeight) + (-x1 - z1) * sprYOffset + blockOffset;
                    break;
                case Facing.South:
                    x2 = midWidth + (x1 - z1) * sprXOffset;
                    y2 = midHeight + (y1 * sprHeight) + (x1 + z1) * sprYOffset + blockOffset;
                    break;
                case Facing.East:
                    x2 = midWidth + (-x1 - z1) * sprXOffset;
                    y2 = midHeight + (y1 * sprHeight) + (x1 - z1) * sprYOffset + blockOffset;
                    break;
                case Facing.West:
                    x2 = midWidth + (x1 + z1) * sprXOffset;
                    y2 = midHeight + (y1 * sprHeight) + (-x1 + z1) * sprYOffset + blockOffset;
                    break;
            }
            z2 = 0.0f;// (x1 + z1 + y1) / (32+32+128);
            return new Tuple<float, float, float>(x2, y2, z2);
        }
        void RenderBlock(float time, BlockType blockType, float x, float y, float z, Position pos)
        {
			int texture = textures [(int)blockType];
			if (texture == 0)
				return;
            GL.PushMatrix();
            GL.Translate(x,y,z);
			GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.CallList(boxListIndex);

            if ( Math.Abs(x - mousePosX) < 16 && Math.Abs(y-mousePosY) < 16)
            {
			    texture = textures [(int)BlockTypeCursor];
			    if (texture == 0)
    				return;
			    GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.CallList(boxListIndex);
                selectedBlock = pos;
            }
            GL.PopMatrix();
        }
        void RenderLargeBlock(float time, BlockType blockType, float x, float y, float z, Position pos)
        {
            int texture = largeTextures[(int)blockType];
            if (texture == 0)
                return;
            GL.PushMatrix();
            GL.Translate(x,y,z);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.CallList(boxListLargeIndex);

            if (Math.Abs(x - mousePosX) < 256 && Math.Abs(y - mousePosY) < 256)
            {
                texture = largeTextures[(int)BlockTypeCursor];
                if (texture == 0)
                    return;
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.CallList(boxListLargeIndex);
                selectedChunk = new ChunkCoords(pos);
                selectedChunkHeight = pos.Y;
            }
            GL.PopMatrix();
        }
        int CompileBox()
        {
            int newList = GL.GenLists(1);
            GL.NewList(newList, ListMode.Compile);

			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //GL.Scale (0.1, 0.1, 1.0);
            float halfSprSize = 16;
            GL.Begin (PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-halfSprSize, -halfSprSize);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(halfSprSize, -halfSprSize);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(halfSprSize, halfSprSize);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-halfSprSize, halfSprSize);
            GL.End ();

            GL.EndList();
            return newList;
        }
        int CompileLargeBox()
        {
            int newList = GL.GenLists(1);
            GL.NewList(newList, ListMode.Compile);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //GL.Scale (0.1, 0.1, 1.0);
            float halfSprSize = Global.CHUNK_SIZE * 16; // 512 for 32 blocks, 256 for 16
            GL.Begin (PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-halfSprSize, -halfSprSize);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(halfSprSize, -halfSprSize);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(halfSprSize, halfSprSize);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-halfSprSize, halfSprSize);
            GL.End ();

            GL.EndList();
            return newList;
        }


		void RenderGui()
		{
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);

			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BindTexture(TextureTarget.Texture2D, renderer.Texture);

			GL.Begin(BeginMode.Quads);

			GL.Color3(Color.White);
			GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-1f, -1f);
			GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(1f, -1f);
			GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(1f, 1f);
			GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1f, 1f);

			GL.End();

			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			//GL.Enable(EnableCap.DepthTest);
		}

	}
}