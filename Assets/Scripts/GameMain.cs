using UnityEngine;

namespace Game1
{
  public class GameMain : MonoBehaviour
  {
    public static GameMain instance { get; private set; }

    public GameConfig config { get; private set; } = new GameConfig();

    public Player player;

    /// <summary>
    /// 日志管理器组件引用
    /// </summary>
    private Logger _logger;

    private void Awake()
    {
      instance = this;
    }

    void Start()
    {

    }

    void OnDestroy()
    {
      
    }

    void Update()
    {

    }
  }

  public class GameConfig
  {
    
  }
}