using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Components;
using Microsoft.Xna.Framework.Input;

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
    private static readonly System.Random nugdeRandom = new();

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

    private bool fxTrail;
    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];

    private readonly List<Texture2D> textures = [];
    private readonly List<int> frameCounts = [];
    private int textureIndex;
    private float frame;

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

    private float hp = 100;

    enum TextureIndex
    {
        Idle,
        Running
    }

    private readonly PlayerInputMapping inputMapping = new() {
        
    };

    public PlayerInputMapping InputMapping => inputMapping;

    public bool UseGamePad { get; set; }
    public PlayerIndex GamePadIndex { get; set; }

    public bool Dead { get; private set; }

    public override void EntityAwake()
    {
        string texPath = "Images/Player/";
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

    void OnStateEnter(PlayerState state)
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

    public override void Update()
    {
        int inputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        bool wasOnGround = OnGround;
        bool onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            jumpCancelled = false;
        }

        RecalculateStats();

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
