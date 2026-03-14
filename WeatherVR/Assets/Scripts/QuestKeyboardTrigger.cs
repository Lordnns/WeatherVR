using UnityEngine;
using TMPro;

public class QuestKeyboardTrigger : MonoBehaviour
{
    private TMP_InputField _inputField;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
        _inputField.onSelect.AddListener(OnSelected);
    }

    private void OnSelected(string value)
    {
        TouchScreenKeyboard.Open(value, TouchScreenKeyboardType.Default);
    }
}