using PassTheLight.Interaction;
using PassTheLight.Optics;
using UnityEngine;

namespace PassTheLight.Core
{
    public class LevelRoot : MonoBehaviour
    {
        [SerializeField] string title;
        [SerializeField] string tip;
        [SerializeField, TextArea] string lesson;
        [SerializeField] int parMoves = 4;
        [SerializeField] Transform hintFrom;
        [SerializeField] Transform hintTo;
        [SerializeField] string quizQuestion;
        [SerializeField] string[] quizOptions = new string[3];
        [SerializeField] int correctOption;
        [SerializeField, TextArea] string quizExplanation;

        StudentDesk[] desks;
        Grabbable[] pieces;

        public string Title => title;
        public string Tip => tip;
        public string Lesson => lesson;
        public int ParMoves => parMoves;
        public Transform HintFrom => hintFrom;
        public Transform HintTo => hintTo;
        public string QuizQuestion => quizQuestion;
        public string[] QuizOptions => quizOptions;
        public int CorrectOption => correctOption;
        public string QuizExplanation => quizExplanation;

        public StudentDesk[] Desks => desks ??= GetComponentsInChildren<StudentDesk>(true);
        Grabbable[] Pieces => pieces ??= GetComponentsInChildren<Grabbable>(true);

        public bool AllLit => LitCount == Desks.Length;

        public int LitCount
        {
            get
            {
                int count = 0;
                foreach (StudentDesk desk in Desks)
                    if (desk.IsLit) count++;
                return count;
            }
        }

        public int StarsFor(int moves)
        {
            if (moves <= parMoves) return 3;
            if (moves <= parMoves + 4) return 2;
            return 1;
        }

        public void ResetLevel()
        {
            foreach (Grabbable piece in Pieces) piece.ResetPose();
            foreach (StudentDesk desk in Desks) desk.ResetState();
        }
    }
}
