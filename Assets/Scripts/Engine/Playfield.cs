﻿using System;
using UnityEngine;
using TetrisEngine.TetriminosPiece;

namespace TetrisEngine
{
	
    public class Playfield
    {
		internal enum SpotState{ EMPTY_SPOT = 0, FILLED_SPOT = 1}
              
		public const int WIDTH = 10;
		public const int HEIGHT = 22;

		public Action OnCurrentPieceReachBottom;
		public Action OnGameOver;
		public Action<int> OnDestroyLine;

		private int[][] mPlayfield = new int[WIDTH][];
		private TetriminoSpawner mSpawner;
		private Tetrimino mCurrentTetrimino;
		private GameSettings mGameSettings;      


		public Playfield(GameSettings gameSettings)
        {
			mGameSettings = gameSettings;

			for (int i = 0; i < WIDTH; i++)
			{
				mPlayfield[i] = new int[HEIGHT];
			}

			ResetGame();
            
			mSpawner = new TetriminoSpawner(mGameSettings.controledRandomMode, mGameSettings.pieces);
        }

		public void ResetGame()
		{
			mCurrentTetrimino = null;
			for (int i = 0; i < WIDTH; i++)
			{
				for (int j = 0; j < HEIGHT; j++)
				{
					mPlayfield[i][j] = (int)SpotState.EMPTY_SPOT;
				}
			}

			if (mGameSettings.debugMode)
                Debug.Log("RESETING GAME");
		}
        
		public Tetrimino CreateTetrimo()
		{
			mCurrentTetrimino = mSpawner.GetRandomTetrimino();
			int rotation = RandomGenerator.random.Next(0, mCurrentTetrimino.blockPositions.GetLength(0));
            Vector2Int position = mCurrentTetrimino.GetInitialPosition(rotation);
			position.x += WIDTH / 2;

			mCurrentTetrimino.currentPosition = position;
			mCurrentTetrimino.currentRotation = rotation;

			if (mGameSettings.debugMode)
				Debug.Log("CREATING TETRIMINO: " + mCurrentTetrimino.name);

			return mCurrentTetrimino;
		}

     
		public void Step()
		{
			if (IsPossibleMovement(mCurrentTetrimino.currentPosition.x, mCurrentTetrimino.currentPosition.y + 1, mCurrentTetrimino, mCurrentTetrimino.currentRotation))
            {
				mCurrentTetrimino.currentPosition = new Vector2Int(mCurrentTetrimino.currentPosition.x, mCurrentTetrimino.currentPosition.y + 1);
            }
            else
            {
				PlaceTetrimino(mCurrentTetrimino);            
                DeletePossibleLines();

                if (IsGameOver())
                {
					if(mGameSettings.debugMode)
                        Debug.Log("GAME OVER");
					
					OnGameOver.Invoke();
					return;
                }

				if(mGameSettings.debugMode)
				    Dump();
				
				OnCurrentPieceReachBottom.Invoke();
            }
		}

		private void PlaceTetrimino(Tetrimino tetrimino)
		{
			for (int i1 = tetrimino.currentPosition.x, i2 = 0; i1 < tetrimino.currentPosition.x + Tetrimino.BLOCK_AREA; i1++, i2++)
            {
				for (int j1 = tetrimino.currentPosition.y, j2 = 0; j1 < tetrimino.currentPosition.y + Tetrimino.BLOCK_AREA; j1++, j2++)
                {
					if(tetrimino.ValidBlock(tetrimino.currentRotation, j2, i2) && InBounds(i1, j1))
					{
						mPlayfield[i1][j1] = (int)SpotState.FILLED_SPOT;
					}
                }
            }
		}

        public bool IsGameOver()
		{
			for (int i = 0; i < WIDTH; i++)
            {
				if (mPlayfield[i][0] == (int)SpotState.FILLED_SPOT)
					return true;
            }

            return false;
        }

 
		private void DeleteLine(int y)
        {      
			if(mGameSettings.debugMode)
                Debug.Log("DESTROYING LINE: " + y);         
            for (int j = y; j > 0; j--)
            {
                for (int i = 0; i < WIDTH; i++)
                {
					mPlayfield[i][j] = mPlayfield[i][j - 1];
                }
            }
			OnDestroyLine.Invoke(y);
        }

		private void DeletePossibleLines()
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                int i = 0;
                while (i < WIDTH)
                {
					if (mPlayfield[i][j] != (int)SpotState.FILLED_SPOT) break;
                    i++;
                }

                if (i == WIDTH) DeleteLine(j);
            }
        }

		private bool IsFreeBlock(int pX, int pY)
        {
			return mPlayfield[pX][pY] == (int)SpotState.EMPTY_SPOT;
        }

   
		public bool IsPossibleMovement(int x, int y, Tetrimino tetrimino, int rotation)
        {
			for (int i1 = x, i2 = 0; i1 < x + Tetrimino.BLOCK_AREA; i1++, i2++)
            {
				for (int j1 = y, j2 = 0; j1 < y + Tetrimino.BLOCK_AREA; j1++, j2++)
                {
                    if (i1 < 0 ||
                        i1 > WIDTH - 1 ||
                        j1 > HEIGHT - 1)
                    {
						if (tetrimino.ValidBlock(rotation, j2, i2))
                            return false;
                    }

                    if (j1 >= 0)
                    {
						if ((tetrimino.ValidBlock(rotation, j2, i2)) &&
                            (!IsFreeBlock(i1, j1)))
                            return false;
                    }
                }
            }

            return true;
        }

		private bool InBounds(int x, int y)
		{
			return x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT;
		}
        
		public void Dump()
		{
			string playfield = "";
			for (int i = 0; i < HEIGHT; i++)
			{
				for (int j = 0; j < WIDTH; j++)
				{
					playfield += string.Format("[{0}]", mPlayfield[j][i]);
				}
				playfield += Environment.NewLine;
			}

			Debug.Log(playfield);
		}      
    }
}
