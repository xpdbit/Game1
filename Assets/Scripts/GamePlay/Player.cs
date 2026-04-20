using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1
{
  public class Player : MonoBehaviour
  {
    public static Player instance => GameMain.instance.player;

    public PlayerInput playerInput;
    
    void Update()
    {
      if (Mouse.current.leftButton.wasPressedThisFrame)
      {
        
      }
    }
  }
}