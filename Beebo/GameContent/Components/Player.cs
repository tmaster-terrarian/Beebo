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
    Dead,
    LedgeGrab,
    LedgeClimb,
    Wallslide,
}

public class Player : Actor
{
    private static readonly Random nugdeRandom = new();

    private PlayerState _state = PlayerState.Normal; // please do NOT touch this thx
    private bool _stateJustChanged;
    private int stateTimer;

    private readonly float gravity = 0.2f;
    private readonly float baseMoveSpeed = 3f;
    private readonly float baseJumpSpeed = -3.7f;
    private readonly float baseGroundAcceleration = 0.1f;
    private readonly float baseGroundFriction = 0.15f;
    private readonly float baseAirAcceleration = 0.07f;
    private readonly float baseAirFriction = 0.05f;

    private float moveSpeed;
    private float jumpSpeed;
    private float accel;
    private float fric;

    private bool useGravity = true;
    private bool jumpCancelled;
    private bool running;
    private bool skidding;
    private int jumpBuffer;
    private int inputDir;
    private bool wasOnGround;
    private bool onJumpthrough;

    private Vector2 oldVelocity;

    private float duck;
    private float lookup;

    private bool canJump;
    private bool canWalljump;

    private bool fxTrail;
    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];

    private readonly List<Texture2D> textures = [];
    private readonly List<int> frameCounts = [];
    private TextureIndex textureIndex;
    private float frame;

    // TIMERS
    private int ledgegrabTimer;
    private int landTimer;

    private float hp = 100;

    private Rectangle MaskNormal = new(-4, -14, 8, 14);
    private Rectangle MaskDuck = new(-4, -6, 8, 6);
    private Rectangle MaskLedge = new(-8, 0, 8, 14);

    private SpriteComponent Sprite => Entity.GetComponent<SpriteComponent>();

    private Solid platformTarget = null;

    class AfterImage
    {
        public float Alpha = 1;
        public TextureIndex TextureIndex;
        public int Frame;
        public Point Position;
        public int Facing;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float Rotation;
        public SpriteEffects SpriteEffects = SpriteEffects.None;
    }

    public enum TextureIndex
    {
        Idle,
        IdleLookUp,
        Crawl,
        Duck,
        Dead,
        Jump,
        Run,
        RunFast,
        Wallslide,
        LedgeGrab,
        LedgeClimb,
    }

    public PlayerInputMapping InputMapping { get; } = new() {
        Left = new MappedInput.Keyboard(Keys.A),
        Right = new MappedInput.Keyboard(Keys.D),
        Up = new MappedInput.Keyboard(Keys.W),
        Down = new MappedInput.Keyboard(Keys.S),
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

    public bool Dead { get; private set; }

    public override void OnCreated()
    {
        AddTexture("idle", 6);
        AddTexture("idle_lookup", 6);
        AddTexture("crawl", 8);
        AddTexture("duck", 4);
        AddTexture("dead", 3);
        AddTexture("jump", 6);
        AddTexture("run", 8);
        AddTexture("run_fast", 6);
        AddTexture("wallslide");
        AddTexture("ledgegrab");
        AddTexture("ledgeclimb", 3);

        SetHitbox(MaskNormal);

        Entity.Depth = 50;
    }

    void AddTexture(string path, int frameCount = 1)
    {
        const string texPath = "Images/Player/";
        textures.Add(Main.LoadContent<Texture2D>(texPath + path));
        frameCounts.Add(frameCount);
    }

    void SetHitbox(Rectangle mask)
    {
        bboxOffset = mask.Location;
        Width = mask.Width;
        Height = mask.Height;
    }

    public override void Update()
    {
        wasOnGround = OnGround;
        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            Grounded();
        }

        if(textureIndex == TextureIndex.Idle)
            frame += 0.2f;

        running = textureIndex == TextureIndex.Run;

        if(State != PlayerState.LedgeClimb)
            ledgegrabTimer = Util.Approach(ledgegrabTimer, 0, 1);

        inputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        if(OnGround && State != PlayerState.LedgeGrab)
        {
            platformTarget = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(0, 2));
        }
        else if(Scene.CollisionSystem.SolidMeeting(Hitbox.Shift(2 * inputDir, 0)) && (State == PlayerState.Normal || State == PlayerState.Wallslide))
        {
            platformTarget = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(2 * inputDir, 0));
        }
        else if(jumpBuffer < 10 && State != PlayerState.LedgeGrab)
        {
            platformTarget = null;
        }

        RecalculateStats();

        if(!OnGround)
            duck = 0;
        if(duck > 0)
        {
            moveSpeed *= 0.5f;
            if(Math.Abs(velocity.X) > moveSpeed)
                velocity.X = Util.Approach(velocity.X, moveSpeed * inputDir, 0.25f);
        }

        StateUpdate();

        if(InputMapping.Jump.Pressed && canJump)
        {
            if(OnGround || (jumpBuffer > 0 && velocity.Y > 0))
            {
                platformTarget = null;
                // var s = null;
                if(duck <= 0)
                {
                    State = PlayerState.Normal;
                    frame = 0;
                    textureIndex = TextureIndex.Jump;
                    var c = Scene.CollisionSystem.SolidPlace(new Rectangle(Center.X, Bottom + 1, 1, 1));
                    if(c is not null)
                    {
                        oldVelocity.X = c.velocity.X;
                        oldVelocity.Y = c.velocity.Y;
                        velocity.X += c.velocity.X;
                        if(c.velocity.Y < 0)
                            velocity.Y = c.velocity.Y;
                    }
                    velocity.Y = jumpSpeed;
                    // s = _audio_play_sound(sn_jump, 0, false);
                }
                else if(!CheckColliding(Hitbox.Shift(0, -2)))
                {
                    State = PlayerState.Normal;
                    var c = Scene.CollisionSystem.SolidPlace(new Rectangle(Center.X, Bottom + 1, 1, 1));
                    if(c is not null)
                    {
                        oldVelocity.X = c.velocity.X;
                        oldVelocity.Y = c.velocity.Y;
                        velocity.X += c.velocity.X;
                        if(c.velocity.Y < 0)
                            velocity.Y = c.velocity.Y;
                    }
                    velocity.Y += jumpSpeed / 2;
                    // s = _audio_play_sound(sn_jump, 0, false);
                }
                else
                {
                    // s = _audio_play_sound(sn_jump, 0, false);
                }

                if(!OnGround)
                {
                    if(CheckColliding(Hitbox.Shift((int)moveSpeed, 0)) && canWalljump)
                    {
                        State = PlayerState.Normal;
                        velocity.X = -moveSpeed;
                        velocity.Y = jumpSpeed;
                        Facing = -1;
                        var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)moveSpeed, 0));
                        if(w is not null)
                            velocity.X += w.velocity.X / 2;
                        // audio_stop_sound(s);
                        // _audio_play_sound(sn_walljump, 0, false);
                    }
                    else if(CheckColliding(Hitbox.Shift((int)-moveSpeed, 0)) && canWalljump)
                    {
                        State = PlayerState.Normal;
                        velocity.X = moveSpeed;
                        velocity.Y = jumpSpeed;
                        Facing = 1;
                        var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)-moveSpeed, 0));
                        if(w is not null)
                            velocity.X += w.velocity.X / 2;
                        // audio_stop_sound(s);
                        // _audio_play_sound(sn_walljump, 0, false);
                    }
                    else if(jumpBuffer > 0 && velocity.Y > 0)
                    {
                        jumpBuffer = 0;
                        for (var i = 0; i < 4; i++)
                        {
                            // var p = instance_create_depth((bbox_left + random(8)), random_range(bbox_bottom, bbox_bottom), (depth - 1), fx_dust);
                            //     p.textureIndex = spr_fx_dust2;
                            //     p.vx = random_range(-0.5, 0.5);
                            //     p.vz = random_range(-0.2, 0);
                        }
                    }
                }
            }
            else if(State == PlayerState.LedgeGrab || State == PlayerState.LedgeClimb)
            {
                ledgegrabTimer = 15;
                Collides = true;
                var c = platformTarget;
                if(c is not null)
                {
                    oldVelocity.X = c.velocity.X;
                    oldVelocity.Y = c.velocity.Y;
                    velocity.X = c.velocity.X;
                    if(c.velocity.Y < 0)
                        velocity.Y = c.velocity.Y;
                }

                SetHitbox(MaskNormal);
                stateTimer = 0;

                if(inputDir != 0) // if input then jump off with some horizontal speed
                {
                    velocity.X += moveSpeed * 0.8f * inputDir + (0.4f * -Facing);

                    if(!CheckColliding(Hitbox.Shift(0, c is not null ? c.Top - Entity.Y : 0))) // if theres space jump as normal
                        velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                    else // else displace the player first
                    {
                        if(State == PlayerState.LedgeGrab)
                            Entity.X -= 4 * Facing;
                        Entity.Y += 12;
                        velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                        textureIndex = TextureIndex.Jump;
                        frame = 0;
                    }
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                else // otherwise just hop off
                {
                    if(State == PlayerState.LedgeGrab)
                        Entity.X -= 4 * Facing;
                    Entity.Y += 12;
                    velocity.Y -= 2.7f * (!InputMapping.Down.IsDown).ToInt32();
                    textureIndex = TextureIndex.Jump;
                    frame = 0;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                State = PlayerState.Normal;
            }
            else if(canWalljump)
            {
                if(CheckColliding(Hitbox.Shift((int)moveSpeed, 0)))
                {
                    platformTarget = null;
                    State = PlayerState.Normal;
                    velocity.X = -moveSpeed;
                    velocity.Y = jumpSpeed;
                    Facing = -1;
                    var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)moveSpeed, 0));
                    if(w is not null)
                        velocity.X += w.velocity.X / 2;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
                else if(CheckColliding(Hitbox.Shift((int)-moveSpeed, 0)))
                {
                    platformTarget = null;
                    State = PlayerState.Normal;
                    velocity.X = moveSpeed;
                    velocity.Y = jumpSpeed;
                    Facing = 1;
                    var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift((int)-moveSpeed, 0));
                    if(w is not null)
                        velocity.X += w.velocity.X / 2;
                    // _audio_play_sound(sn_walljump, 0, false);
                }
            }
        }

        if(InputMapping.Jump.Released && velocity.Y < 0 && !jumpCancelled)
        {
            jumpCancelled = true;
            velocity.Y /= 2;
        }

        // if(velocity.Y > 4)
        // {
        //     squash = clamp(1.01 * (velocity.Y / 12), 1, 1.4)
        //     stretch = clamp(0.99 / (velocity.Y / 12), 0.65, 1)
        // }

        if(skidding && OnGround)
        {
            textureIndex = TextureIndex.Run;
            frame = 6;

            // with(instance_create_depth(x, bbox_bottom, (depth - 10), fx_dust))
            // {
            //     sprite_index = spr_fx_dust2;
            //     image_index = irandom(1)
            //     vx = random_range(-0.1, 0.1);
            //     vy = random_range(-0.5, -0.1);
            //     vz = 0;
            // }
        }

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
            if(State == PlayerState.Dead)
            {
                velocity.X = -velocity.X * 0.9f;
            }
            else for(int j = 0; j < Util.RoundToInt(MathHelper.Max(Time.DeltaTime, 1)); j++)
            {
                if(inputDir != 0 && !CheckColliding(Hitbox.Shift(inputDir, -2)))
                {
                    if(CheckColliding(Hitbox.Shift(inputDir, 0)))
                    {
                        MoveY(-2, null);
                        MoveX(inputDir * 2, null);
                    }
                }
                else
                {
                    // if (Math.Abs(velocity.X) >= 1)
                    // {
                    //     _audio_play_sound(sn_player_land, 0, false);
                    //     for (int i = 0; i < 3; i++)
                    //     {
                    //         with(instance_create_depth((x + (4 * sign(facing))), random_range((bbox_bottom - 12), (bbox_bottom - 2)), (depth - 1), fx_dust))
                    //         {
                    //             sprite_index = spr_fx_dust2;
                    //             vy = (Math.Abs(other.vsp) > 0.6) ? other.vsp * 0.5 : vy;
                    //             vz = 0;
                    //         }
                    //     }
                    // }
                    velocity.X = 0;
                    break;
                }
            }
        });
        MoveY(velocity.Y, () => {
            if(State == PlayerState.Normal)
            {
                landTimer = 8;
                textureIndex = inputDir != 0 ? TextureIndex.Run : TextureIndex.Idle;
                frame = 0;
            }
            // if (velocity.Y > 0.4f)
            // {
            //     _audio_play_sound(sn_player_land, 0, false);
            //     squash = 0.9;
            //     stretch = 1.4;
            // }
            // if (velocity.Y > 0.2)
            // {
            //     for (var i = 0; i < 4; i++)
            //     {
            //         with (instance_create_depth((bbox_left + random(8)), random_range(bbox_bottom, bbox_bottom), (depth - 1), fx_dust))
            //         {
            //             sprite_index = spr_fx_dust2
            //             vx = other.velocity.X
            //             vz = 0
            //         }
            //     }
            // }
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

        if(inputDir != 0 && running && CheckColliding(Hitbox.Shift(inputDir, 0)))
        {
            textureIndex = TextureIndex.Idle;
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
                    textureIndex = TextureIndex.Idle;
                    frame = 0;
                }
                else
                {
                    textureIndex = TextureIndex.Jump;
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

                    if(inputDir * velocity.X < 0)
                    {
                        if(Math.Abs(velocity.X) > moveSpeed * 0.6f)
                            skidding = true;
                        else
                            skidding = false;

                        velocity.X = Util.Approach(velocity.X, 0, fric);
                    }
                    else if(OnGround && velocity.Y >= 0)
                    {
                        skidding = false;
                        running = true;
                        if (duck == 0 && landTimer == 0)
                        {
                            if(Math.Abs(velocity.X) > moveSpeed * 1.3)
                                textureIndex = TextureIndex.RunFast;
                            else
                                textureIndex = TextureIndex.Run;
                        }
                        else if(duck > 0)
                        {
                            textureIndex = TextureIndex.Crawl;
                        }
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
                    velocity.X = Util.Approach(velocity.X, oldVelocity.X, fric * 2);

                    if(OnGround && Math.Abs(velocity.X) < 1.5f)
                    {
                        textureIndex = TextureIndex.Idle;
                    }

                    if (Math.Abs(velocity.X) < moveSpeed)
                    {
                        skidding = false;
                        // run = approach(run, 0, global.dt)
                    }
                    if (Math.Abs(velocity.X) < 1.5f && OnGround && landTimer <= 0)
                    {
                        textureIndex = TextureIndex.Idle;
                        if(duck > 0)
                        {
                            textureIndex = TextureIndex.Duck;
                            frame = duck;
                            lookup = -0.5f;
                        }
                        else if(InputMapping.Up.IsDown)
                        {
                            textureIndex = TextureIndex.IdleLookUp;
                            lookup = 1;
                        }
                        else
                        {
                            lookup = 0;
                        }
                    }
                }

                if (InputMapping.Down.IsDown && OnGround)
                    duck = Util.Approach(duck, 3, 1);
                else if(!CheckColliding(Hitbox.Shift(0, -6)))
                {
                    duck = Util.Approach(duck, 0, 1);
                }

                if(!OnGround)
                {
                    lookup = 0;

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
                    frame += Math.Abs(velocity.X) / frameCounts[(int)textureIndex] / 2.5f;
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

    private void Grounded()
    {
        jumpCancelled = false;
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
            if(Math.Abs(velocity.X) > moveSpeed * 1.3)
                fric *= 0.5f;
        }
    }

    public override void Draw()
    {
        foreach(var image in afterImages)
        {
            if(!Visible) continue;

            var texture = textures[(int)image.TextureIndex];
            int width = texture.Width / frameCounts[(int)image.TextureIndex];
            Rectangle drawFrame = new(image.Frame * width, 0, width, texture.Height);

            if(JellyBackend.DebugEnabled)
            {
                Renderer.SpriteBatch.DrawNineSlice(
                    Main.LoadContent<Texture2D>("Images/Debug/tileOutline"),
                    new Rectangle(image.Position.X, image.Position.Y, Width, Height),
                    null,
                    new Point(1),
                    new Point(1),
                    Color.Blue * 0.75f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0
                );
            }

            Renderer.SpriteBatch.Draw(
                texture,
                (image.Position + new Point(texture.Width / 2, texture.Height / 2)).ToVector2(),
                drawFrame,
                image.Color * (image.Alpha * 0.5f),
                image.Rotation,
                new Vector2(width / 2, texture.Height / 2),
                image.Scale,
                image.SpriteEffects,
                0
            );
        }

        {
            var texture = textures[(int)textureIndex];
            int width = texture.Width / frameCounts[(int)textureIndex];
            Rectangle drawFrame = new((int)frame * width, 0, width, texture.Height);

            Renderer.SpriteBatch.Draw(
                texture,
                Entity.Position.ToVector2(), drawFrame,
                Color.White,
                0, Entity.Pivot.ToVector2(),
                1, SpriteEffects,
                0
            );
        }

        if(JellyBackend.DebugEnabled)
        {
            Renderer.SpriteBatch.DrawNineSlice(Main.LoadContent<Texture2D>("Images/Debug/tileOutline"), Hitbox, null, new Point(1), new Point(1), Color.Red * 0.5f);
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
