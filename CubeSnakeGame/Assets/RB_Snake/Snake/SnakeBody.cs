﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RBSnake
{
    public class SnakeBody : MonoBehaviour
    {
        [Header("Init")]
        public bool CheckPointLoaded;

        [Header("Parts")]
        public SnakeBody Front;
        public SnakeBody Back;
        
        private Control control;

        private void Start()
        {
            control = FindObjectOfType<Control>();
        }

        void SpawnRatDeathEffects(Vector3 pos)
        {
            GameObject blood = PoolManager.Instance.GetObject(PoolObjectType.VFX_BLOOD);
            GameObject rat = PoolManager.Instance.GetObject(PoolObjectType.VFX_RAT);

            blood.transform.position = pos;
            blood.gameObject.SetActive(true);

            rat.transform.position = pos;
            rat.gameObject.SetActive(true);
        }

        void SpawnSnakeDeathEffects(Vector3 pos)
        {
            GameObject blood = PoolManager.Instance.GetObject(PoolObjectType.VFX_BLOOD);
            GameObject snake = PoolManager.Instance.GetObject(PoolObjectType.VFX_SNAKE);

            blood.transform.position = pos;
            blood.gameObject.SetActive(true);

            snake.transform.position = pos;
            snake.gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            CollisionAgainstFood(other);
            CollisionAgainstSelf(other);
            CollisionAgainstWater(other);
            CollisionAgainstTrap(other);
            CollisionAgainstCheckPoint(other);
        }

        void CollisionAgainstFood(Collider other)
        {
            if (other.gameObject.GetComponent<Rat>() != null)
            {
                SpawnTail();
                Destroy(other.gameObject);
                SpawnRatDeathEffects(this.transform.position);

                // camera shake
                CameraManager.Instance.CAMERA_CONTROL.ShakeCamera();
            }
        }

        void CollisionAgainstSelf(Collider other)
        {
            if (this == SnakeBodyManager.Instance.SNAKE_HEAD)
            {
                if (!SnakeBodyManager.Instance.FirstUpdate)
                {
                    SnakeBody collidedBody = other.gameObject.GetComponent<SnakeBody>();

                    if (collidedBody != null)
                    {
                        SnakeBodyManager.Instance.PLAYER.IsDead = true;
                        SpawnSnakeDeathEffects(this.transform.position);
                        CameraManager.Instance.CAMERA_CONTROL.ZoomInAndOut(0.15f, 0.2f);
                        other.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
        }

        void CollisionAgainstWater(Collider other)
        {
            if (other.transform.parent != null)
            {
                if (other.transform.parent.gameObject.GetComponent<Water>() != null)
                {
                    // head face downward
                    if (!SnakeBodyManager.Instance.PLAYER.IsDrowning)
                    {
                        if (Front == null)
                        {
                            Vector3 newup = this.transform.forward;
                            this.transform.LookAt(this.transform.position + new Vector3(0f, -1f, 0f), newup);
                        }
                    }

                    SnakeBodyManager.Instance.PLAYER.IsDead = true;
                    SnakeBodyManager.Instance.PLAYER.IsDrowning = true;
                    control.KeyPresses.Clear();
                }
            }
        }

        void CollisionAgainstTrap(Collider other)
        {
            if (other.transform.parent != null)
            {
                if (other.transform.parent.gameObject.GetComponent<Trap>() != null)
                {
                    SnakeBodyManager.Instance.PLAYER.IsDead = true;
                }
            }
        }

        void CollisionAgainstCheckPoint(Collider other)
        {
            CheckPoint cp = other.transform.GetComponent<CheckPoint>();
            if (cp != null)
            {
                UnlockCheckpoint(cp);
            }
        }

        void SpawnTail()
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>("SnakeBody")) as GameObject;
            obj.transform.position = SnakeBodyManager.Instance.Bodies[0].transform.position;

            SnakeBody newBody = obj.GetComponent<SnakeBody>();

            SnakeBodyManager.Instance.Bodies.Insert(0, newBody);
            SnakeBodyManager.Instance.Bodies[0].Front = SnakeBodyManager.Instance.Bodies[1];
            SnakeBodyManager.Instance.Bodies[1].Back = SnakeBodyManager.Instance.Bodies[0];

            newBody.ScaleDown();
            newBody.CheckPointLoaded = true;
        }

        public void RegisterBody()
        {
            if (!SnakeBodyManager.Instance.Bodies.Contains(this))
            {
                SnakeBodyManager.Instance.Bodies.Add(this);

                // recursive: repeat the same process until you reach the head 
                if (Front != null)
                {
                    Front.RegisterBody();
                    ScaleDown();
                }
            }
        }

        public void ScaleDown()
        {
            if (this.transform.localScale.x >= 0.5f)
            {
                this.transform.localScale = Front.transform.localScale * 0.925f;
            }
        }

        public void InitBodyOnCheckPoint()
        {
            if (!CheckPointLoaded)
            {
                int cpIndex = SaveManager.Instance.checkPointLoader.sceneSelection.LastSelectedCheckpoint;

                foreach (CheckPoint p in SaveManager.Instance.PointsList)
                {
                    if (p.Index == cpIndex)
                    {
                        this.transform.position = p.transform.position;
                        break;
                    }
                }

                CheckPointLoaded = true;
            }
        }

        public void UpdateBody()
        {
            if (control.LastPress == KeyCode.None)
            {
                return;
            }

            if (Front != null)
            {
                if (Front.transform.position == this.transform.position)
                {
                    return;
                }
                else
                {
                    //move body (follow front)
                    this.transform.position = Front.transform.position;
                }
            }
            else
            {
                if (control.KeyPresses.Count != 0)
                {
                    //move head (key press)
                    MoveDir(control.KeyPresses[0]);
                }
                else
                {
                    //continue moving the head (no key press)
                    this.transform.position += this.transform.forward;
                }
            }
        }

        public void MoveDir(KeyCode key)
        {
            if (key == KeyCode.LeftArrow)
            {
                this.transform.position -= Vector3.right;
                this.transform.forward = -Vector3.right;
            }

            if (key == KeyCode.RightArrow)
            {
                this.transform.position += Vector3.right;
                this.transform.forward = Vector3.right;
            }

            if (key == KeyCode.UpArrow)
            {
                this.transform.position += Vector3.forward;
                this.transform.forward = Vector3.forward;
            }

            if (key == KeyCode.DownArrow)
            {
                this.transform.position -= Vector3.forward;
                this.transform.forward = -Vector3.forward;
            }

            control.KeyPresses.RemoveAt(0);
        }

        void UnlockCheckpoint(CheckPoint cp)
        {
            List<int> list = SaveManager.Instance.checkPointLoader.sceneSelection.
                GetUnlockedCheckPoints(cp.IslandType);

            if (!list.Contains(cp.Index))
            {
                list.Add(cp.Index);
            }

            SaveManager.Instance.SaveData();
        }
    }
}