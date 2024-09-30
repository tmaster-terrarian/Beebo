using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Components;
using Microsoft.Xna.Framework.Input;
using System;
using Jelly.Graphics;

namespace Beebo.GameContent.Components;

public enum PlayerState
{
    IgnoreState,
    StandIdle,
    Normal,
    Dead
}

public class Player : Actor
{
    private static readonly Random nugdeRandom = new();

    private PlayerState _state = PlayerState.Normal; // please do NOT touch this thx
    private bool _stateJustChanged;
    private float stateTimer;

    private readonly float gravity = 0.2f;
    private readonly float baseMoveSpeed = 3f;
    private readonly float baseJumpSpeed = -4.5f;
    private readonly float baseGroundAcceleration = 0.15f;
    private readonly float baseGroundFriction = 0.1f;
    private readonly float baseAirAcceleration = 0.07f;
    private readonly float baseAirFriction = 0.05f;

    private float moveSpeed;
    private float jumpSpeed;
    private float accel;
    private float fric;

    private bool useGravity = true;
    private bool jumpCancelled;
    private bool running;
    private int inputDir;
    private bool wasOnGround;
    private bool onJumpthrough;

    private bool fxTrail;
    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];

    private readonly List<Texture2D> textures = [];
    private readonly List<int> frameCounts = [];
    private int textureIndex;
    private float frame;

    private float hp = 100;

    private SpriteComponent Sprite => Entity.GetComponent<SpriteComponent>();

    class AfterImage
    {
        public float Alpha = 1;
        public int TextureIndex;
        public int Frame;
        public Point Position;
        public int Facing;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float Rotation;
    }

    enum TextureIndex
    {
        Idle,
        Running
    }

    public PlayerInputMapping InputMapping { get; } = new() {
        
    };

    public PlayerState State {
        get => _state;
        set {
            if(value < 0 || value > PlayerState.Dead) value = PlayerState.IgnoreState;

            if(_state != value)
            {
                _stateJustChanged = true;
                stateTimer = 0;

                OnStateExit(_state);
                OnStateEnter(value);

                _state = value;
            }
        }
    }

    protected bool FaceTowardsMouse { get; set; }

    public bool UseGamePad { get; set; }
    public PlayerIndex GamePadIndex { get; set; }

    public bool Dead { get; private set; }

    public override void OnCreated()
    {
        string texPath = "Images/Players/";
        void addTex(string path, int count = 1) => AddTexture(Main.LoadContent<Texture2D>(texPath + path), count);

        addTex("idle");
        addTex("run", 6);

        Entity.Depth = 50;
    }

    void AddTexture(Texture2D texture, int frameCount = 1)
    {
        textures.Add(texture);
        frameCounts.Add(frameCount);
    }

    public override void Update()
    {
        inputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        wasOnGround = OnGround;
        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            jumpCancelled = false;
        }

        RecalculateStats();

        StateUpdate();

        if(!OnGround && useGravity)
        {
            if(velocity.Y >= 0.1)
                velocity.Y = Util.Approach(velocity.Y, 20, gravity);
            if(velocity.Y < 0)
                velocity.Y = Util.Approach(velocity.Y, 20, gravity);
            else if (velocity.Y < 2)
                velocity.Y = Util.Approach(velocity.Y, 20, gravity * 0.25f);
        }

        MoveX(velocity.X, () => {
            velocity.X = 0;
        });
        MoveY(velocity.Y, () => {
            if(!(InputMapping.Down.IsDown && CheckCollidingJumpthrough(BottomEdge.Shift(new(0, 1)))))
                velocity.Y = 0;
        });

        if(fxTrail)
        {
            fxTrailCounter++;
            if(fxTrailCounter >= 2)
            {
                fxTrailCounter = 0;
                afterImages.Add(new AfterImage {
                    TextureIndex = textureIndex,
                    Frame = (int)frame,
                    Position = Entity.Position,
                    Facing = Facing,
                    Scale = Sprite.Scale,
                    Color = Sprite.Color,
                    Rotation = Sprite.Rotation
                });
            }
        }
        else
        {
            fxTrailCounter = 0;
        }

        for(int i = 0; i < afterImages.Count; i++)
        {
            AfterImage image = afterImages[i];

            image.Alpha = MathHelper.Max(image.Alpha - (1/12f), 0);
            if(image.Alpha == 0)
            {
                afterImages.RemoveAt(i);
                i--;
            }
        }
    }

    private void OnStateEnter(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.IgnoreState:
                break;
            case PlayerState.StandIdle:
                if(OnGround)
                {
                    textureIndex = (int)TextureIndex.Idle;
                    frame = 0;
                }
                else
                {
                    textureIndex = (int)TextureIndex.Idle; // jump texture, replace later pls
                }
                break;
            case PlayerState.Normal:
                break;
            case PlayerState.Dead:
                break;
            default:
                break;
        }
    }

    private void StateUpdate()
    {
        if(!_stateJustChanged)
        {
            useGravity = false;
            CollidesWithJumpthroughs = true;
            CollidesWithSolids = true;
        }
        else
        {
            _stateJustChanged = false;
        }

        switch(State)
        {
            case PlayerState.StandIdle:
                useGravity = true;

                velocity.X = Util.Approach(velocity.X, 0, fric * 2);

                if(OnGround)
                {
                    textureIndex = (int)TextureIndex.Idle;
                }

                break;
            case PlayerState.Normal:
                useGravity = true;

                if(inputDir != 0)
                {
                    Facing = inputDir;

                    if(FaceTowardsMouse)
                        Facing = Math.Sign(Main.Camera.MousePositionInWorld.X - Center.X);

                    if(OnGround)
                    {
                        running = true;

                        if(velocity.Y >= 0)
                        {
                            textureIndex = (int)TextureIndex.Running;
                        }
                    }

                    if(inputDir * velocity.X < 0)
                    {
                        velocity.X = Util.Approach(velocity.X, 0, fric);
                    }
                    if(inputDir * velocity.X < moveSpeed)
                    {
                        velocity.X = Util.Approach(velocity.X, inputDir * moveSpeed, accel);
                    }

                    if(inputDir * velocity.X > moveSpeed && OnGround)
                    {
                        velocity.X = Util.Approach(velocity.X, inputDir * moveSpeed, fric/3);
                    }
                }
                else
                {
                    running = false;
                    velocity.X = Util.Approach(velocity.X, 0, fric * 2);

                    if(OnGround && Math.Abs(velocity.X) < 1.5f)
                    {
                        textureIndex = (int)TextureIndex.Idle;
                    }
                }

                if(!OnGround)
                {
                    if(InputMapping.Down.Released && velocity.Y < 0 && !jumpCancelled)
                    {
                        jumpCancelled = true;
                        velocity.Y /= 2;
                    }
                }
                else
                {
                    if(onJumpthrough && InputMapping.Down.IsDown && !CheckColliding(BottomEdge.Shift(new(0, 2)), true))
                    {
                        Entity.Y += 2;

                        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
                        if(onJumpthrough) OnGround = true;
                        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));
                    }
                }

                if(running)
                {
                    frame += Math.Abs(velocity.X) / frameCounts[textureIndex] / 2.5f;
                }

                fxTrail = Math.Abs(velocity.X) > 1f * moveSpeed;

                // ...

                if(OnGround && InputMapping.Jump.Pressed)
                {
                    velocity.Y = jumpSpeed;
                }

                break;
            case PlayerState.IgnoreState: default:
                break;
        }
    }

    private void OnStateExit(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.IgnoreState:
                break;
            case PlayerState.StandIdle:
                break;
            case PlayerState.Normal:
                break;
            case PlayerState.Dead:
                break;
        }
    }

    private void RecalculateStats()
    {
        moveSpeed = baseMoveSpeed;
        jumpSpeed = baseJumpSpeed;

        accel = baseGroundAcceleration;
        fric = baseGroundFriction;
        if(!OnGround)
        {
            accel = baseAirAcceleration;
            fric = baseAirFriction;
        }
    }
}

public class PlayerInputMapping
{
    public MappedInput Right { get; set; } = new MappedInput.Keyboard(Keys.D);
    public MappedInput Left { get; set; } = new MappedInput.Keyboard(Keys.A);
    public MappedInput Down { get; set; } = new MappedInput.Keyboard(Keys.S);
    public MappedInput Up { get; set; } = new MappedInput.Keyboard(Keys.W);
    public MappedInput Jump { get; set; } = new MappedInput.Keyboard(Keys.Space);
    public MappedInput PrimaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.LeftButton);
    public MappedInput SecondaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.RightButton);
}
