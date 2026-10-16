using System.Collections;
using PassTheLight.Core;
using PassTheLight.Interaction;
using PassTheLight.Optics;
using UnityEngine;
using UnityEngine.UI;

namespace PassTheLight.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] Button[] levelButtons;
        [SerializeField] Image[] levelStars;
        [SerializeField] GameObject[] levelLocks;
        [SerializeField] Button resetProgressButton;

        [Header("HUD")]
        [SerializeField] GameObject hudPanel;
        [SerializeField] Text levelTitleText;
        [SerializeField] Image[] levelDots;
        [SerializeField] Image[] studentIcons;
        [SerializeField] Text movesText;
        [SerializeField] Text starTargetText;
        [SerializeField] Button homeButton;
        [SerializeField] Button pauseButton;
        [SerializeField] Button helpButton;
        [SerializeField] Text tipText;
        [SerializeField] GameObject anglePanel;
        [SerializeField] Text angleText;
        [SerializeField] Button rotateLeftButton;
        [SerializeField] Button rotateRightButton;
        [SerializeField] RectTransform hintArrow;

        [Header("Music")]
        [SerializeField] Button[] musicButtons;
        [SerializeField] Image[] musicIcons;
        [SerializeField] Sprite musicOnSprite;
        [SerializeField] Sprite musicOffSprite;

        [Header("Lesson card")]
        [SerializeField] GameObject lessonPanel;
        [SerializeField] Text lessonTitleText;
        [SerializeField] Text lessonBodyText;
        [SerializeField] Text lessonGoalText;
        [SerializeField] Button lessonCloseButton;

        [Header("Quiz")]
        [SerializeField] GameObject quizPanel;
        [SerializeField] RectTransform quizCard;
        [SerializeField] Text quizQuestionText;
        [SerializeField] Text quizFeedbackText;
        [SerializeField] Button[] quizOptionButtons;
        [SerializeField] Image[] quizOptionMarks;
        [SerializeField] Sprite checkSprite;
        [SerializeField] Sprite crossSprite;

        [Header("Level complete")]
        [SerializeField] GameObject completePanel;
        [SerializeField] Text completeStatsText;
        [SerializeField] Image[] completeStars;
        [SerializeField] Button nextButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button completeHomeButton;

        [Header("Pause")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;

        [Header("Finish")]
        [SerializeField] GameObject finalPanel;
        [SerializeField] Button finalMenuButton;

        [Header("Colours")]
        [SerializeField] Color gold = new(1f, 0.78f, 0.25f);
        [SerializeField] Color dim = new(1f, 1f, 1f, 0.18f);
        [SerializeField] Color correctColor = new(0.25f, 0.7f, 0.4f);
        [SerializeField] Color wrongColor = new(0.85f, 0.3f, 0.3f);

        GameManager game;
        PointerInteractor interactor;
        Camera cam;
        LevelRoot currentLevel;
        int studentsShownAsLit;
        ColorBlock optionColors;

        public void Init(GameManager gameManager, PointerInteractor pointer)
        {
            game = gameManager;
            interactor = pointer;
            cam = Camera.main;
            optionColors = quizOptionButtons[0].colors;
            ConnectButtons();
            RefreshMusicIcons();
        }

        void ConnectButtons()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelButtons[i].onClick.AddListener(() => game.StartLevel(index));
            }
            for (int i = 0; i < quizOptionButtons.Length; i++)
            {
                int option = i;
                quizOptionButtons[i].onClick.AddListener(() => game.AnswerQuiz(option));
            }
            foreach (Button button in musicButtons)
                button.onClick.AddListener(() => { Sfx.ToggleMusic(); RefreshMusicIcons(); });

            resetProgressButton.onClick.AddListener(() => { ProgressStore.ResetProgress(levelButtons.Length); ShowMenu(); });
            homeButton.onClick.AddListener(game.ShowMenu);
            pauseButton.onClick.AddListener(game.TogglePause);
            helpButton.onClick.AddListener(OpenLesson);
            lessonCloseButton.onClick.AddListener(() => lessonPanel.SetActive(false));
            rotateLeftButton.onClick.AddListener(() => interactor.RotateSelected(-1f));
            rotateRightButton.onClick.AddListener(() => interactor.RotateSelected(1f));
            nextButton.onClick.AddListener(game.NextLevel);
            replayButton.onClick.AddListener(game.RestartLevel);
            completeHomeButton.onClick.AddListener(game.ShowMenu);
            resumeButton.onClick.AddListener(game.TogglePause);
            restartButton.onClick.AddListener(game.RestartLevel);
            menuButton.onClick.AddListener(game.ShowMenu);
            finalMenuButton.onClick.AddListener(game.ShowMenu);
        }

        void LateUpdate()
        {
            if (!hudPanel.activeSelf) return;
            UpdateAngleReadout();
            UpdateHintArrow();

            bool canRotate = interactor.Selected && game.State == GameState.Playing;
            rotateLeftButton.interactable = canRotate;
            rotateRightButton.interactable = canRotate;
        }

        public void ShowMenu()
        {
            ShowOnly(menuPanel);
            for (int level = 0; level < levelButtons.Length; level++)
            {
                bool unlocked = level < ProgressStore.UnlockedLevels;
                levelButtons[level].interactable = unlocked;
                levelLocks[level].SetActive(!unlocked);

                int starsEarned = ProgressStore.BestStars(level);
                for (int star = 0; star < 3; star++)
                {
                    Image starImage = levelStars[level * 3 + star];
                    starImage.gameObject.SetActive(unlocked);
                    starImage.color = star < starsEarned ? gold : dim;
                }
            }
        }

        public void ShowLevel(LevelRoot level, int index)
        {
            ShowOnly(hudPanel);
            currentLevel = level;
            levelTitleText.text = level.Title;
            tipText.text = level.Tip;
            starTargetText.text = $"in {level.ParMoves} moves";
            for (int i = 0; i < levelDots.Length; i++)
            {
                levelDots[i].color = i <= index ? gold : dim;
                levelDots[i].transform.localScale = Vector3.one * (i == index ? 1.3f : 1f);
            }
            studentsShownAsLit = 0;
            OpenLesson();
        }

        void OpenLesson()
        {
            if (!currentLevel) return;
            lessonTitleText.text = currentLevel.Title;
            lessonBodyText.text = currentLevel.Lesson;
            int studentCount = currentLevel.Desks.Length;
            string plural = studentCount > 1 ? "S" : "";
            lessonGoalText.text = $"GOAL: LIGHT {studentCount} STUDENT{plural}";
            lessonPanel.SetActive(true);
            Sfx.Pop();
        }

        public void SetStudents(int lit, int total)
        {
            for (int i = 0; i < studentIcons.Length; i++)
            {
                studentIcons[i].gameObject.SetActive(i < total);
                studentIcons[i].color = i < lit ? gold : dim;
            }
            for (int i = studentsShownAsLit; i < lit; i++)
                StartCoroutine(PopScale(studentIcons[i].transform, 1.6f));
            studentsShownAsLit = lit;
        }

        public void SetMoves(int moves) => movesText.text = moves.ToString();

        public void ShowQuiz(LevelRoot level)
        {
            lessonPanel.SetActive(false);
            quizPanel.SetActive(true);
            Sfx.Pop();
            quizQuestionText.text = level.QuizQuestion;
            quizFeedbackText.text = "";
            for (int i = 0; i < quizOptionButtons.Length; i++)
            {
                quizOptionButtons[i].interactable = true;
                quizOptionButtons[i].colors = optionColors;
                quizOptionMarks[i].gameObject.SetActive(false);
                SetLabel(quizOptionButtons[i], level.QuizOptions[i]);
            }
        }

        public void ShowQuizFeedback(int option, bool correct, string message)
        {
            quizFeedbackText.text = message;
            quizFeedbackText.color = correct ? correctColor : wrongColor;

            Color answerColor = correct ? correctColor : wrongColor;
            ColorBlock colors = optionColors;
            colors.normalColor = answerColor;
            colors.highlightedColor = answerColor;
            colors.selectedColor = answerColor;
            colors.disabledColor = answerColor;
            quizOptionButtons[option].colors = colors;

            Image mark = quizOptionMarks[option];
            mark.sprite = correct ? checkSprite : crossSprite;
            mark.gameObject.SetActive(true);
            StartCoroutine(PopScale(mark.transform, 1.5f));

            if (correct)
                foreach (Button button in quizOptionButtons) button.interactable = false;
            else
                StartCoroutine(Shake(quizCard));
        }

        public void ShowComplete(int moves, int stars, bool newBest, bool isLast)
        {
            quizPanel.SetActive(false);
            completePanel.SetActive(true);
            completeStatsText.text = newBest ? $"{moves} moves  ·  new best!" : $"{moves} moves";
            nextButton.gameObject.SetActive(!isLast);
            foreach (Image star in completeStars)
            {
                star.color = dim;
                star.transform.localScale = Vector3.one;
            }
            StartCoroutine(RevealStars(stars, isLast));
        }

        public void ShowPause(bool paused)
        {
            pausePanel.SetActive(paused);
            if (paused) Sfx.Pop();
        }

        public void ShowFinal()
        {
            ShowOnly(finalPanel);
            Sfx.LevelLit();
        }

        void UpdateAngleReadout()
        {
            Mirror mirror = interactor.Selected ? interactor.Selected.GetComponent<Mirror>() : null;
            anglePanel.SetActive(mirror != null);
            if (!mirror) return;
            if (!mirror.IsInBeam)
            {
                angleText.text = "Not in the beam";
                return;
            }
            int angle = Mathf.RoundToInt(mirror.LastIncidenceAngle);
            angleText.text = $"i {angle}°   r {angle}°";
        }

        void UpdateHintArrow()
        {
            bool levelHasHint = currentLevel && currentLevel.HintFrom && currentLevel.HintTo;
            bool playerHasNotMoved = game.Moves == 0 && game.State == GameState.Playing && !lessonPanel.activeSelf;
            bool showArrow = levelHasHint && playerHasNotMoved;
            hintArrow.gameObject.SetActive(showArrow);
            if (!showArrow) return;

            bool pointAtMirror = Mathf.Repeat(Time.unscaledTime, 2.4f) < 1.2f;
            Transform target = pointAtMirror ? currentLevel.HintFrom : currentLevel.HintTo;
            Vector2 screenPoint = cam.WorldToScreenPoint(target.position + Vector3.up * 1.2f);

            var hud = (RectTransform)hintArrow.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, screenPoint, null, out Vector2 hudPoint);
            float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 25f;
            hintArrow.anchoredPosition = hudPoint + new Vector2(0f, 40f + bounce);
        }

        void RefreshMusicIcons()
        {
            foreach (Image icon in musicIcons) icon.sprite = Sfx.MusicOn ? musicOnSprite : musicOffSprite;
        }

        IEnumerator RevealStars(int stars, bool isLast)
        {
            for (int i = 0; i < stars; i++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                completeStars[i].color = gold;
                Sfx.Star(i);
                StartCoroutine(PopScale(completeStars[i].transform, 1.5f));
            }
            if (isLast)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                game.NextLevel();
            }
        }

        static IEnumerator PopScale(Transform target, float startScale)
        {
            const float duration = 0.3f;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float scale = Mathf.Lerp(startScale, 1f, time / duration);
                target.localScale = Vector3.one * scale;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        static IEnumerator Shake(RectTransform target)
        {
            Vector2 startPosition = target.anchoredPosition;
            const float duration = 0.4f;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float strength = 20f * (1f - time / duration);
                float offset = Mathf.Sin(time * 60f) * strength;
                target.anchoredPosition = startPosition + new Vector2(offset, 0f);
                yield return null;
            }
            target.anchoredPosition = startPosition;
        }

        void ShowOnly(GameObject panel)
        {
            menuPanel.SetActive(panel == menuPanel);
            hudPanel.SetActive(panel == hudPanel);
            finalPanel.SetActive(panel == finalPanel);
            lessonPanel.SetActive(false);
            quizPanel.SetActive(false);
            completePanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        static void SetLabel(Button button, string text) => button.GetComponentInChildren<Text>().text = text;
    }
}
