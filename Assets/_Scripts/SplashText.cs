using UnityEngine;
using UnityEngine.UI;
using TMPro; // if you’re using TextMeshPro

public class SplashText : MonoBehaviour
{
    public enum SplashOptions
    {
        BlatantPlagiarism,
        WeOutHere,
        ISmellCheese,
    }

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI splashText; // swap for UnityEngine.UI.Text if you’re not using TMP

    private void Start()
    {
        // get all enum values
        SplashOptions[] values = (SplashOptions[])System.Enum.GetValues(typeof(SplashOptions));

        // pick a random one
        SplashOptions randomChoice = values[Random.Range(0, values.Length)];

        // set the text
        splashText.text = ToReadableString(randomChoice);
    }

    // optional: make enum names prettier
    private string ToReadableString(SplashOptions option)
    {
        switch (option)
        {
            case SplashOptions.BlatantPlagiarism: return "BLATANT PLAGIARISM";
            case SplashOptions.WeOutHere: return "WE OUT HERE BOY!";
            case SplashOptions.ISmellCheese: return "I Smell Cheese?";
            default: return option.ToString();
        }
    }
}
