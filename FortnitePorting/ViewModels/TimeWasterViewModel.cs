using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Models.TimeWaster;
using FortnitePorting.Models.TimeWaster.Actors;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;
using TWPlayer = FortnitePorting.Models.TimeWaster.Actors.TWPlayer;

namespace FortnitePorting.ViewModels;

public partial class TimeWasterViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isGame = true;
    
    [ObservableProperty] private ObservableCollection<TWProjectile> _projectiles = [];
    [ObservableProperty] private ObservableCollection<TWObstacle> _obstacles = [];
    [ObservableProperty] private ObservableCollection<TWObstacleExplosion> _obstacleExplosions = [];
    [ObservableProperty] private ObservableCollection<TWPineapple> _pineapples = [];
    [ObservableProperty] private TWPlayer _player = new();
    [ObservableProperty] private TWPlayerExplosion _playerExplosion = new();
    [ObservableProperty] private TWBoss _boss = new();
    [ObservableProperty] private TWVictoryText _victoryText = new();
    [ObservableProperty] private int _score = 0;
    [ObservableProperty] private bool _fightingBoss = false;

    [ObservableProperty] private Rect _viewportBounds = new(0, 0, 1160, 770);
    
    [ObservableProperty] private double _backgroundRotation = 0;
    [ObservableProperty] private double _barPosition = 0;
    [ObservableProperty] private TransformGroup _starsTransform;
    [ObservableProperty] private TransformGroup _blackHoleTransform;
    [ObservableProperty] private TransformGroup _flareTransform;
    [ObservableProperty] private TransformGroup _spaceTransform;
    [ObservableProperty] private TransformGroup _barsTransform;
    
    [ObservableProperty] private ScaleTransform _scoreTextTransform = new(1, 1);

    private float TimeSinceLastProjectile;
    private int NextBossScore = BOSS_SCORE_DISTANCE;
    
    private readonly WaveOutEvent AmbientOutput = new();
    private readonly WaveOutEvent GameOutput = new();
    private static LoopStream AmbientBackground;
    private static LoopStream GameBackground;
    private static CachedSound Spawn;
    private static CachedSound Shoot;
    private static CachedSound Explode;
    private static CachedSound Death;
    private static CachedSound BossAppear;
    private static CachedSound BossHit;
    private static CachedSound Win;
    private static List<CachedSound> PianoSnippets = [];
    
    private const int ACTOR_DESPAWN_MARGIN = 200;
    private const int OBSTACLE_SPAWN_MARGIN = 100;
    private const int OBSTACLE_INTERACT_MARGIN = 40;
    private const int BOSS_INTERACT_MARGIN = 125;
    private const int BOSS_SCORE_DISTANCE = 3000;
    private const int BOSS_SCORE = 5000;
    private const float DELTA_TIME = 1.0f / 60f;

    public static void LoadResources()
    { 
        AmbientBackground = new LoopStream(new VorbisWaveReader(AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/TimeWaster/Music/Ambient_Music.ogg"))));
        GameBackground = new LoopStream(new VorbisWaveReader(AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/TimeWaster/Music/Game_Music.ogg"))));
        Spawn = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_Spawn_01.ogg");
        Shoot = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_Shoot_01.ogg");
        Explode = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_Explo_01.ogg");
        Death = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_Death_01.ogg");
        BossAppear = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_BossAppear_01.ogg");
        BossHit = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_BossHit_01.ogg");
        Win = new CachedSound("avares://FortnitePorting/Assets/TimeWaster/SFX/PMB_Win_01.ogg");

        for (var index = 1; index <= 8; index++)
        {
            PianoSnippets.Add(new CachedSound($"avares://FortnitePorting/Assets/TimeWaster/Music/PianoSnippets/NightNight_Music_PianoSnip_{index:D2}.ogg"));
        }
    }
    
    public override async Task Initialize()
    {
        if (Design.IsDesignMode) return;
        
        RegisterUpdater(UpdateBackground);
        InitAudio(AmbientOutput, AmbientBackground);

        TaskService.Run(async () =>
        {
            while (!IsGame)
            {
                var waitTime = Random.Shared.Next(10000, 30000);
                await Task.Delay(waitTime);
                
                if (IsGame) break;
                if (!BlackHole.IsActive) break;
                
                PianoSnippets.Random()?.Play();
            }
        });

        if (IsGame)
        {
            await InitializeGame();
        }
        
    }

    [RelayCommand]
    public void Exit()
    {
        BlackHole.Close();
    }

    public async Task InitializeGame()
    {
        await TaskService.RunDispatcherAsync(Player.Initialize);
        RegisterUpdater(UpdatePlayer);
        RegisterUpdater(UpdateProjectiles);
        RegisterUpdater(UpdateObstacles);
        RegisterUpdater(UpdateObstacleExplosions);
        RegisterUpdater(UpdateBoss);
        RegisterUpdater(ProjectileTimerHandler);
        RegisterUpdater(CreateObstacles, seconds: 1.4f);
        
        Spawn.Play();

        InitAudio(GameOutput, GameBackground);
    }

    public override async Task OnViewExited()
    {
        CleanupResources();
    }

    public override void OnApplicationExit()
    {
        CleanupResources();
    }

    public void CleanupResources()
    {
        AudioSystem.Instance.Stop();
        Updaters.ForEach(updater => updater.Stop());
        AmbientOutput.Stop();
        AmbientOutput.Dispose();
        GameOutput.Stop();
        GameOutput.Dispose();
    }


    private void UpdateBoss()
    {
        VictoryText.Update();
        Boss.Update();

        foreach (var pineapple in Pineapples.ToArray())
        {
            if (Math.Abs(Player.Position.X - pineapple.Position.X) <= OBSTACLE_INTERACT_MARGIN
                && Math.Abs(Player.Position.Y - pineapple.Position.Y) <= OBSTACLE_INTERACT_MARGIN
                && Player is { Dead: false, IsVulnerable: true })
            {
                Pineapples.Remove(pineapple);
                PlayerExplosion.Execute(Player.Position);
                Death.Play();
                
                Player.Kill();
                TaskService.RunDispatcher(async () =>
                {
                    await Task.Delay(4000);
                    Player.Spawn();
                    Spawn.Play();
                });
            }

            pineapple.Update();
        }
        
        if (!FightingBoss && Score == NextBossScore)
        {
            FightingBoss = true;
            NextBossScore += BOSS_SCORE_DISTANCE;
            TaskService.Run(async () =>
            {
                await Task.Delay(500);
                Boss.Spawn();
                await Task.Delay(1000);
                BossAppear.Play();
            });
        }

        
        if (Boss.IsActive
            && Projectiles.FirstOrDefault(proj => Math.Abs(proj.Position.X - Boss.Position.X) <= BOSS_INTERACT_MARGIN 
            && Math.Abs(proj.Position.Y - Boss.Position.Y) <= BOSS_INTERACT_MARGIN) is { } projectile)
        {
            Projectiles.Remove(projectile);
            if (Boss.State is EBossState.Spawning) return;
            
            BossHit.Play();
            Boss.Health--;
            
            if (Boss.Health <= 0) // dub
            {
                Boss.Kill();
                Explode.Play();
                Win.Play();
                OnObstacleDestroyed(Boss, score: BOSS_SCORE, scale: 3);
                FightingBoss = false;
                NextBossScore += BOSS_SCORE;

                VictoryText.Display();
                
                return;
            }

            TaskService.Run(async () =>
            {
                Boss.HitOpacity = 0.5;
                await Task.Delay(250);
                Boss.HitOpacity = 0.0;
            });
        }
    }

    private void ProjectileTimerHandler()
    {
        TimeSinceLastProjectile += DELTA_TIME;
    }

    public void ShootProjectile()
    {
        if (Player.Dead) return;
        if (!Player.FinishedSpawn) return;
        if (TimeSinceLastProjectile <= 0.2f) return;
        
        var projectile = new TWProjectile
        {
            Position =
            {
                X = Player.Position.X,
                Y = Player.Position.Y
            },
            Scale =
            {
                Y = 1.2
            }
        };

        projectile.Initialize();
        Projectiles.Add(projectile);
        Shoot.Play();
        TimeSinceLastProjectile = 0;
    }
    
    private void UpdateObstacleExplosions()
    {
        foreach (var explosion in ObstacleExplosions.ToArray())
        {
            if (explosion.Time > 0.5f)
            {
                ObstacleExplosions.Remove(explosion);
                continue;
            }
            
            explosion.Update();
        }
    }
    
    private void UpdateObstacles()
    {
        var playedBossExplode = false;
        foreach (var obstacle in Obstacles.ToArray())
        {
            if (Boss.IsActive)
            {
                Obstacles.Remove(obstacle);
                OnObstacleDestroyed(obstacle, score: 0);
                if (!playedBossExplode)
                {
                    Explode.Play();
                    playedBossExplode = true;
                }
                continue;
            }
            
            if (obstacle.Position.Y > ViewportBounds.Width / 2 + ACTOR_DESPAWN_MARGIN)
            {
                Obstacles.Remove(obstacle);
                continue;
            }

            if (Projectiles.FirstOrDefault(proj => Math.Abs(proj.Position.X - obstacle.Position.X) <= OBSTACLE_INTERACT_MARGIN 
                                                   && Math.Abs(proj.Position.Y - obstacle.Position.Y) <= OBSTACLE_INTERACT_MARGIN) is { } projectile)
            {
                Obstacles.Remove(obstacle);
                Projectiles.Remove(projectile);
                Explode.Play();

                OnObstacleDestroyed(obstacle);
                continue;
            }
            
            if (Math.Abs(Player.Position.X - obstacle.Position.X) <= OBSTACLE_INTERACT_MARGIN 
                && Math.Abs(Player.Position.Y - obstacle.Position.Y) <= OBSTACLE_INTERACT_MARGIN
                && Player is { Dead: false, IsVulnerable: true })
            {
                Obstacles.Remove(obstacle);
                PlayerExplosion.Execute(Player.Position);
                Death.Play();
                
                Player.Kill();
                TaskService.RunDispatcher(async () =>
                {
                    await Task.Delay(4000);
                    Player.Spawn();
                    Spawn.Play();
                });
                
                continue;
            }
            
            obstacle.Update();
        }
    }

    private void UpdateProjectiles()
    {
        foreach (var projectile in Projectiles.ToArray())
        {
            if (projectile.Position.Y < -ViewportBounds.Height - ACTOR_DESPAWN_MARGIN)
            {
                Projectiles.Remove(projectile);
                continue;
            }
            
            projectile.Update();
        }
    }

    private void UpdatePlayer()
    {
        Player.Update();
        PlayerExplosion.Update();
    }

    private void UpdateBackground()
    {
        BackgroundRotation -= 0.05;
        BarPosition += 0.25;
        
        StarsTransform = new TransformGroup
        {
            Children =
            [
                Create3DRotation(0, 0, BackgroundRotation, 45, 45, 45),
                new ScaleTransform(0.7, 0.7)
            ]
        };
        
        BlackHoleTransform = new TransformGroup
        {
            Children =
            [
                Create3DRotation(0, 0, BackgroundRotation * -0.4, centerX: -2.5, centerY: 5),
                new ScaleTransform(1.0, 1.0)
            ]
        };
        
        FlareTransform = new TransformGroup
        {
            Children =
            [
                Create3DRotation(25, 25, BackgroundRotation * 2),
                new ScaleTransform(0.6, 0.6)
            ]
        };
        
        SpaceTransform = new TransformGroup
        {
            Children =
            [
                Create3DRotation(25, 45, BackgroundRotation * 0.8, centerY: 10),
                new ScaleTransform(0.4, 0.4)
            ]
        };
        
        BarsTransform = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(0.02, 0.02),
                new TranslateTransform(0, BarPosition)
            ]
        };
    }
    
    private void CreateObstacles()
    {
        if (FightingBoss) return;
        
        var x = Random.Shared.Next((int) (-ViewportBounds.Width / 2) + OBSTACLE_SPAWN_MARGIN, (int) (ViewportBounds.Width / 2) - OBSTACLE_SPAWN_MARGIN);
        var y = (int) (-ViewportBounds.Height / 2 - OBSTACLE_SPAWN_MARGIN);
        var angularVelocity = 0.035 * (Random.Shared.NextDouble() + 0.5) * (Random.Shared.Next(0, 2) == 1 ? 1 : -1);
        
        var obstacle = new TWObstacle
        {
            Position =
            {
                X = x,
                Y = y,
            },
            AngularVelocity = angularVelocity,
        };
        
        obstacle.Initialize();
        Obstacles.Add(obstacle);
    }

    private void OnObstacleDestroyed(TWActor obstacle, int score = 100, double scale = 1.0)
    {
        var explosion = new TWObstacleExplosion
        {
            Position = obstacle.Position.Copy(),
            Score = score,
            Scale = scale
        };
        
        explosion.Initialize();
        ObstacleExplosions.Add(explosion);

        if (score > 0)
        {
            TaskService.RunDispatcher(async () =>
            {
                ScoreTextTransform = new ScaleTransform(1.25, 1.25);
                await Task.Delay(400);
                ScoreTextTransform = new ScaleTransform(1, 1);
            });
        }
        
        Score += score;
    }
    
    private static Rotate3DTransform Create3DRotation(double x, double y, double z, 
        double centerX = 0, double centerY = 0, double centerZ = 0, int depth = 2000)
    {
        return new Rotate3DTransform(x, y, z, centerX, centerY, centerZ, depth);
    }
    
    private void InitAudio(WaveOutEvent waveOut, LoopStream wave)
    {
        TaskService.Run(async () =>
        {
            wave.Position = 0;
            waveOut.Init(wave);
            waveOut.Play();

            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(25);
            }
        });
    }

    private static List<DispatcherTimer> Updaters = [];
    
    private static void RegisterUpdater(Action action, float seconds = 1.0f / 60f)
    {
        var timer = new DispatcherTimer(TimeSpan.FromSeconds(seconds), DispatcherPriority.Normal,
            (sender, args) => action());
        
        timer.Start();
        Updaters.Add(timer);
    }
}