using UnityEngine;
using TetrisEngine.TetriminosPiece;
using System.Collections.Generic;
using pooling;

namespace TetrisEngine
{   
	
	public class GameLogic : MonoBehaviour 
    {
		private const string JSON_PATH = @"SupportFiles/GameSettings";

		public GameObject tetriminoBlockPrefab;
		public Transform tetriminoParent;
              
        [Header("This property will be overriten by GameSettings.json file.")] 
		[Space(-10)]
		[Header("You can play with it while the game is in Play-Mode.")] 
		public float timeToStep = 2f;

		private GameSettings mGameSettings;
		private Playfield mPlayfield;
		private List<TetriminoView> mTetriminos = new List<TetriminoView>();
		private float mTimer = 0f;
        
		private Pooling<TetriminoBlock> mBlockPool = new Pooling<TetriminoBlock>();    
		private Pooling<TetriminoView> mTetriminoPool = new Pooling<TetriminoView>();

		private Tetrimino mCurrentTetrimino
		{
			get
			{
				return (mTetriminos.Count > 0 && !mTetriminos[mTetriminos.Count - 1].isLocked) ? mTetriminos[mTetriminos.Count - 1].currentTetrimino : null;
			}
		}

		private TetriminoView mPreview;
		private bool mRefreshPreview;
		private bool mGameIsOver;

     
		public void Start()
		{
			mBlockPool.createMoreIfNeeded = true;
			mBlockPool.Initialize(tetriminoBlockPrefab, null);
   
			mTetriminoPool.createMoreIfNeeded = true;
			mTetriminoPool.Initialize(new GameObject("BlockHolder", typeof(RectTransform)), tetriminoParent);
			mTetriminoPool.OnObjectCreationCallBack += x =>
			{
				x.OnDestroyTetrimoView = DestroyTetrimino;
				x.blockPool = mBlockPool;
			};

         
            var settingsFile = Resources.Load<TextAsset>(JSON_PATH);
            if (settingsFile == null)
				throw new System.Exception(string.Format("GameSettings.json could not be found inside {0}. Create one in Window>GameSettings Creator.", JSON_PATH));

           
            var json = settingsFile.text;
            mGameSettings = JsonUtility.FromJson<GameSettings>(json);
			mGameSettings.CheckValidSettings();
			timeToStep = mGameSettings.timeToStep;

			mPlayfield = new Playfield(mGameSettings);
			mPlayfield.OnCurrentPieceReachBottom = CreateTetrimino;
			mPlayfield.OnGameOver = SetGameOver;
			mPlayfield.OnDestroyLine = DestroyLine;

			GameOver.instance.HideScreen(0f);
			Score.instance.HideScreen();
                     
			RestartGame();
		}

        
        public void RestartGame()
		{
			GameOver.instance.HideScreen();
			Score.instance.ResetScore();

            mGameIsOver = false;
			mTimer = 0f;
            
			mPlayfield.ResetGame();
			mTetriminoPool.ReleaseAll();
			mTetriminos.Clear();

            CreateTetrimino();         
		}
        
        
		private void DestroyLine(int y)
		{
			Score.instance.AddPoints(mGameSettings.pointsByBreakingLine);
            
			mTetriminos.ForEach(x => x.DestroyLine(y));
            mTetriminos.RemoveAll(x => x.destroyed == true);
		}

       
		private void SetGameOver()
		{
			mGameIsOver = true;
			GameOver.instance.ShowScreen();
		}

        private void CreateTetrimino()
		{
			if (mCurrentTetrimino != null)
				mCurrentTetrimino.isLocked = true;
			
			var tetrimino = mPlayfield.CreateTetrimo();
			var tetriminoView = mTetriminoPool.Collect();         
			tetriminoView.InitiateTetrimino(tetrimino);
			mTetriminos.Add(tetriminoView);

			if (mPreview != null)
				mTetriminoPool.Release(mPreview);
			
			mPreview = mTetriminoPool.Collect();
			mPreview.InitiateTetrimino(tetrimino, true);
			mRefreshPreview = true;
		}

		private void DestroyTetrimino(TetriminoView obj)
		{
			var index = mTetriminos.FindIndex(x => x == obj);
			mTetriminoPool.Release(obj);
			mTetriminos[index].destroyed = true;
		}

		
		public void Update()
		{
			if (mGameIsOver) return;

			mTimer += Time.deltaTime;
			if(mTimer > timeToStep)
			{
				mTimer = 0;
				mPlayfield.Step();
			}

			if (mCurrentTetrimino == null) return;

			if(Input.GetKeyDown(mGameSettings.rotateRightKey))
			{
				if(mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x,
    											  mCurrentTetrimino.currentPosition.y,
    											  mCurrentTetrimino,
    			                                  mCurrentTetrimino.NextRotation))
				{
					mCurrentTetrimino.currentRotation = mCurrentTetrimino.NextRotation;
					mRefreshPreview = true;
				}
			}

			if (Input.GetKeyDown(mGameSettings.rotateLeftKey))
            {
                if (mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x,
                                                  mCurrentTetrimino.currentPosition.y,
                                                  mCurrentTetrimino,
    			                                  mCurrentTetrimino.PreviousRotation))
                {
					mCurrentTetrimino.currentRotation = mCurrentTetrimino.PreviousRotation;
					mRefreshPreview = true;
                }
            }

			if (Input.GetKeyDown(mGameSettings.moveLeftKey))
            {
                if (mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x - 1,
                                                  mCurrentTetrimino.currentPosition.y,
                                                  mCurrentTetrimino,
                                                  mCurrentTetrimino.currentRotation))
                {
                    mCurrentTetrimino.currentPosition = new Vector2Int(mCurrentTetrimino.currentPosition.x - 1, mCurrentTetrimino.currentPosition.y);
					mRefreshPreview = true;
                }
            }

			if (Input.GetKeyDown(mGameSettings.moveRightKey))
            {
                if (mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x + 1,
                                                  mCurrentTetrimino.currentPosition.y,
                                                  mCurrentTetrimino,
                                                  mCurrentTetrimino.currentRotation))
                {
                    mCurrentTetrimino.currentPosition = new Vector2Int(mCurrentTetrimino.currentPosition.x + 1, mCurrentTetrimino.currentPosition.y);
					mRefreshPreview = true;
                }
            }

  
			if (Input.GetKey(mGameSettings.moveDownKey))
            {
                if (mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x,
                                                  mCurrentTetrimino.currentPosition.y + 1,
                                                  mCurrentTetrimino,
				                                  mCurrentTetrimino.currentRotation))
                {
					mCurrentTetrimino.currentPosition = new Vector2Int(mCurrentTetrimino.currentPosition.x, mCurrentTetrimino.currentPosition.y + 1);               
                }
            }

			if(mRefreshPreview)
			{
				var y = mCurrentTetrimino.currentPosition.y;
				while(mPlayfield.IsPossibleMovement(mCurrentTetrimino.currentPosition.x,
                                                  y,
                                                  mCurrentTetrimino,
                                                  mCurrentTetrimino.currentRotation))
				{
					y++;
				}

				mPreview.ForcePosition(mCurrentTetrimino.currentPosition.x, y - 1);
				mRefreshPreview = false;
			}
		}
	}
}
