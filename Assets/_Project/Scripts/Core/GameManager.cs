using System.Collections;
using PassTheLight.Interaction;
using PassTheLight.Optics;
using PassTheLight.UI;
using UnityEngine;

namespace PassTheLight.Core
{
    public enum GameState { Menu, Playing, Paused, Quiz, LevelComplete, Finished }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] LevelRoot[] levels;
        [SerializeField] UIManager ui;
        [SerializeField] PointerInteractor interactor;
        [SerializeField] ParticleSystem confetti;
        [SerializeField] CameraBump cameraBump;
        [SerializeField] float confirmDelay = 0.5f;
        [SerializeField] float celebrateDelay = 1.6f;

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public int Moves { get; private set; }

        LevelRoot currentLevel;
        int currentLevelIndex;
        Coroutine celebration;

        void Awake()
        {
            Instance = this;
            Physics.queriesHitTriggers = false;
            Application.targetFrameRate = 60;
            foreach (LevelRoot level in levels) level.gameObject.SetActive(false);
        }

        void Start()
        {
            ui.Init(this, interactor);
            ShowMenu();
        }

        public void ShowMenu()
        {
            CloseCurrentLevel();
            Time.timeScale = 1f;
            SetState(GameState.Menu);
            ui.ShowMenu();
        }

        public void StartLevel(int index)
        {
            CloseCurrentLevel();
            Time.timeScale = 1f;

            currentLevelIndex = index;
            currentLevel = levels[index];
            currentLevel.gameObject.SetActive(true);
            currentLevel.ResetLevel();
            foreach (StudentDesk desk in currentLevel.Desks) desk.LitChanged += OnStudentLitChanged;

            Moves = 0;
            SetState(GameState.Playing);
            ui.ShowLevel(currentLevel, index);
            ui.SetStudents(currentLevel.LitCount, currentLevel.Desks.Length);
            ui.SetMoves(Moves);
        }

        public void RestartLevel() => StartLevel(currentLevelIndex);

        public void NextLevel()
        {
            bool hasNextLevel = currentLevelIndex + 1 < levels.Length;
            if (hasNextLevel)
            {
                StartLevel(currentLevelIndex + 1);
                return;
            }

            CloseCurrentLevel();
            SetState(GameState.Finished);
            ui.ShowFinal();
        }

        public void RegisterMove()
        {
            if (State != GameState.Playing) return;
            Moves++;
            ui.SetMoves(Moves);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Paused);
                Time.timeScale = 0f;
                ui.ShowPause(true);
            }
            else if (State == GameState.Paused)
            {
                SetState(GameState.Playing);
                Time.timeScale = 1f;
                ui.ShowPause(false);
                CelebrateIfEveryoneIsLit();
            }
        }

        public void AnswerQuiz(int option)
        {
            if (State != GameState.Quiz) return;

            bool correct = option == currentLevel.CorrectOption;
            if (correct)
            {
                Sfx.Correct();
                ui.ShowQuizFeedback(option, true, currentLevel.QuizExplanation);
                StartCoroutine(FinishLevel());
            }
            else
            {
                Sfx.Wrong();
                ui.ShowQuizFeedback(option, false, "Not quite. Try again!");
            }
        }

        IEnumerator FinishLevel()
        {
            SetState(GameState.LevelComplete);
            yield return new WaitForSecondsRealtime(2f);

            int stars = currentLevel.StarsFor(Moves);
            bool newBest = ProgressStore.TrySaveBest(currentLevelIndex, Moves);
            ProgressStore.SaveStars(currentLevelIndex, stars);
            ProgressStore.Unlock(currentLevelIndex + 2);

            bool isLastLevel = currentLevelIndex == levels.Length - 1;
            ui.ShowComplete(Moves, stars, newBest, isLastLevel);
        }

        void OnStudentLitChanged(StudentDesk desk)
        {
            ui.SetStudents(currentLevel.LitCount, currentLevel.Desks.Length);
            CelebrateIfEveryoneIsLit();
        }

        void CelebrateIfEveryoneIsLit()
        {
            if (State == GameState.Playing && celebration == null && currentLevel.AllLit)
                celebration = StartCoroutine(CelebrateThenShowQuiz());
        }

        IEnumerator CelebrateThenShowQuiz()
        {
            yield return new WaitForSeconds(confirmDelay);

            bool stillAllLit = State == GameState.Playing && currentLevel.AllLit;
            if (!stillAllLit)
            {
                celebration = null;
                yield break;
            }

            SetState(GameState.Quiz);
            Sfx.LevelLit();
            confetti.Play();
            cameraBump.Bump();

            yield return new WaitForSeconds(celebrateDelay);
            celebration = null;
            ui.ShowQuiz(currentLevel);
        }

        void SetState(GameState state)
        {
            State = state;
            interactor.InteractionEnabled = state == GameState.Playing;
        }

        void CloseCurrentLevel()
        {
            if (celebration != null)
            {
                StopCoroutine(celebration);
                celebration = null;
            }
            if (currentLevel == null) return;

            foreach (StudentDesk desk in currentLevel.Desks) desk.LitChanged -= OnStudentLitChanged;
            currentLevel.gameObject.SetActive(false);
            currentLevel = null;
        }
    }
}
