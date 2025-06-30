using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PaylineButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private int num;
  [SerializeField] internal TMP_Text my_Text;
  [SerializeField] private Button my_Button;

  private void Awake()
  {
    if (!int.TryParse(gameObject.name, out num))
    {
      Debug.LogError("Error: PaylineButton.cs: Awake(): num is not a number");
    }
    my_Text = transform.GetChild(0).GetComponent<TMP_Text>();
    my_Button = GetComponent<Button>();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (!slotManager.SocketConnected)
    {
      return;
    }
    slotManager.GenerateStaticLine(num);
  }
  public void OnPointerExit(PointerEventData eventData)
  {
    if (!slotManager.SocketConnected)
    {
      return;
    }
    slotManager.DestroyStaticLine();
  }
  public void OnPointerDown(PointerEventData eventData)
  {
    if (!slotManager.SocketConnected)
    {
      return;
    }
    if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
    {
      my_Button.Select();
      slotManager.GenerateStaticLine(num);
    }
  }
  public void OnPointerUp(PointerEventData eventData)
  {
    if (!slotManager.SocketConnected)
    {
      return;
    }
    if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
    {
      slotManager.DestroyStaticLine();
      DOVirtual.DelayedCall(0.1f, () =>
      {
        my_Button.spriteState = default;
        EventSystem.current.SetSelectedGameObject(null);
      });
    }
  }
}
