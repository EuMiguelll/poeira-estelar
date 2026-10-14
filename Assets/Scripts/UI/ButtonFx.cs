using UnityEngine;
using UnityEngine.EventSystems;

// Som e leve aumento de tamanho quando o botão é selecionado (mouse, teclado ou controle).
public class ButtonFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler, IPointerClickHandler
{
    public AudioClip hoverClip;
    public AudioClip clickClip;
    public float selectedScale = 1.06f;

    bool highlighted;

    void OnDisable()
    {
        highlighted = false;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        float target = highlighted ? selectedScale : 1f;
        float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void Highlight(bool on)
    {
        if (on && !highlighted && MusicPlayer.Instance != null) MusicPlayer.Instance.PlayUi(hoverClip, 0.6f);
        highlighted = on;
    }

    public void OnSelect(BaseEventData eventData) => Highlight(true);
    public void OnDeselect(BaseEventData eventData) => Highlight(false);

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData) { }

    public void OnSubmit(BaseEventData eventData) => Click();
    public void OnPointerClick(PointerEventData eventData) => Click();

    void Click()
    {
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.PlayUi(clickClip);
    }
}
