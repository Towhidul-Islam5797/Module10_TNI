using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorBox : MonoBehaviour, IInteractable
{
    [SerializeField] private Color[] colors = { Color.white, Color.red, Color.green, Color.blue, Color.yellow };

    private Renderer boxRenderer;
    private int colorIndex;

    public string PromptText => "Press E to change color";

    private void Awake()
    {
        boxRenderer = GetComponent<Renderer>();
    }

    public void Interact(PlayerInteractor interactor)
    {
        colorIndex = (colorIndex + 1) % colors.Length;
        boxRenderer.material.color = colors[colorIndex];
    }
}
