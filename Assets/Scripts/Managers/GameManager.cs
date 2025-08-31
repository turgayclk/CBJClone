using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    [Header("Levels")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private ParticleSystem[] confettiSystem;
    [SerializeField] private int invokeEveryNLevels = 2; // Her N level ge�i�inde invoke

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float levelTime = 60f;
    private float remainingTime;
    private bool isTimerRunning = true;

    [Header("UI")]
    [SerializeField] private GameObject failUI;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject TimerUI;
    [SerializeField] private Button nextLevelButton; // butonu inspector�dan ba�la

    public static event System.Action AddCameraCenterPos;

    private bool isLevelCompleted = false;
    private int currentLevelIndex = 0;
    private bool isTransitioning = false;


    private void Awake()
    {
        // Butona t�klama eventini koddan ba�la
        nextLevelButton.onClick.AddListener(NextLevel);

    }

    private void Start()
    {

        LoadLevel(currentLevelIndex);
    }

    private void Update()
    {
        if (isLevelCompleted) return;

        // Timer
        if (isTimerRunning)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime < 0f) remainingTime = 0f;

            if (remainingTime > 60f)
            {
                // Dakika:saniye format�
                System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(remainingTime);
                timerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            }
            else
            {
                // Sadece saniye
                timerText.text = Mathf.CeilToInt(remainingTime).ToString();
            }

            if (remainingTime <= 0f)
            {
                TimerFinished();
                return;
            }
        }

        // Level tamamlanma kontrol�
        if (!isTransitioning && gridManager.GetRemainingBlockCount() == 0 && !isLevelCompleted)
        {
            StartCoroutine(DelayLevelCompleted());
        }
    }

    private void TimerFinished()
    {
        isTimerRunning = false;
        isLevelCompleted = true;

        levelCompleteUI.SetActive(false);
        gridManager.gameObject.SetActive(false);
        TimerUI.SetActive(false);

        failUI.SetActive(true);

        Debug.Log("S�re bitti! Level Failed!");
    }

    public void RetryLevel()
    {
        failUI.SetActive(false);
        isLevelCompleted = false;
        levelCompleteUI.SetActive(false);
        gridManager.gameObject.SetActive(true);
        TimerUI.SetActive(true);

        LoadLevel(currentLevelIndex);
    }

    private void LoadLevel(int index)
    {
        if (index >= 0 && index < levels.Length)
        {
            gridManager.LoadLevel(levels[index]);

            // Timer resetle
            remainingTime = levelTime;
            isTimerRunning = true;
            isLevelCompleted = false;

            // UI g�ncelle
            levelText.text = "Level " + (index + 1);

            levelCompleteUI.SetActive(false);
            failUI.SetActive(false);
            TimerUI.SetActive(true);

            Debug.Log("Level y�klendi: " + index);
        }
    }

    private IEnumerator DelayLevelCompleted()
    {
        yield return new WaitForSeconds(0.5f);
        LevelCompleted();
    }

    private IEnumerator ShowNextLevelButtonWithDelay(float delay)
    {
        nextLevelButton.gameObject.SetActive(false); // g�venlik i�in kapal� tut
        yield return new WaitForSeconds(delay);
        nextLevelButton.gameObject.SetActive(true);
    }

    private void LevelCompleted()
    {
        if (isLevelCompleted || !isTimerRunning) return;

        isLevelCompleted = true;
        isTimerRunning = false;

        TimerUI.SetActive(false);
        gridManager.gameObject.SetActive(false);

        Debug.Log("Level Tamamland�!");
        levelCompleteUI.SetActive(true);

        // Sonra butonu 1 saniye gecikmeli a�
        StartCoroutine(ShowNextLevelButtonWithDelay(1f));

        foreach (var confetti in confettiSystem)
        {
            confetti.Play();
        }
    }

    public void NextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        Debug.Log($"�u anki level: {currentLevelIndex}, ge�ilecek level: {nextIndex}");

        if (nextIndex < levels.Length)
        {

            if (currentLevelIndex == 4)
            {
                AddCameraCenterPos?.Invoke();
            }

            if (nextIndex % 2 == 0)
            {
                AddCameraCenterPos?.Invoke();
            }

            currentLevelIndex = nextIndex; // index g�ncelle
            isTransitioning = true;

            LoadLevel(currentLevelIndex);
            levelCompleteUI.SetActive(false);
            gridManager.gameObject.SetActive(true);

            StartCoroutine(ReenableCheck());
        }
        else
        {
            Debug.Log("T�m leveller bitti!");

            levelCompleteUI.SetActive(false);

            gameOverUI.SetActive(true);
        }
    }

    public void StartAgainButton()
    {
        Debug.Log("Oyuna yeniden ba�land�!");

        // GameOver ekran�n� kapat
        gameOverUI.SetActive(false);

        // Confetti�leri durdur
        foreach (var confetti in confettiSystem)
        {
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // �lk leveli y�kle
        currentLevelIndex = 0;
        LoadLevel(currentLevelIndex);

        // Grid�i yeniden aktif et
        gridManager.gameObject.SetActive(true);

        // UI resetle
        levelCompleteUI.SetActive(false);
        failUI.SetActive(false);
        TimerUI.SetActive(true);

        // Timer s�f�rla
        remainingTime = levelTime;
        isTimerRunning = true;
        isLevelCompleted = false;
        isTransitioning = false;
    }

    private IEnumerator ReenableCheck()
    {
        yield return new WaitForSeconds(1f);
        isTransitioning = false;
    }

}
