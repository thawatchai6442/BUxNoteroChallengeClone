﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace DataStore.Quiz
{
    public class QuizStore
    {
        public event Action<byte[]> OnCustomDataChange;
        public event Action<QuizInfo> OnQuizInfoChange;
        public event Action<int, bool> OnQuizStateChange;
        public event Action<Question> OnQuestionChange;
        public event Action<string> OnAnswerChange;
        public event Action OnStudentAnswerChange;
        public event Action<int> OnCorrectAnswerAmountChange;
        public event Action<PreTestResult> OnPreTestResultChange;

        [JsonProperty]
        public byte[] CustomData { get; private set; }

        [JsonProperty]
        public Question CurrentQuestion { get; private set; }

        [JsonProperty]
        public QuizInfo QuizInfo { get; private set; }

        [JsonProperty]
        public string CorrectAnswer { get; private set; }

        [JsonProperty]
        public int CurrentQuizState { get; private set; }

        [JsonIgnore]
        public int CorrectAnswerAmount { get; private set; }

        [JsonIgnore]
        public PreTestResult PreTestResult { get; private set; }

        [JsonIgnore]
        public Dictionary<string, Question> QuizList => m_QuizList;

        private Dictionary<string, Question> m_QuizList = new();

        [JsonIgnore]
        public Dictionary<string, List<StudentAnswer>> StudentAnswers => m_StudentAnswers;

        private Dictionary<string, List<StudentAnswer>> m_StudentAnswers = new();

        [JsonIgnore]
        public Dictionary<string, int> StudentPreTestResult => m_StudentPreTestResult;

        private Dictionary<string, int> m_StudentPreTestResult = new();

        [JsonIgnore]
        public int StudentAmount
        {
            get
            {
                if(CurrentQuestion.Id == null || m_StudentAnswers == null || !m_StudentAnswers.TryGetValue(CurrentQuestion.Id, out var studentAnswers)) return 0;

                return studentAnswers.Count;
            }
        }

        [JsonIgnore]
        public int AnswerStudentAmount
        {
            get
            {
                if(CurrentQuestion.Id == null || m_StudentAnswers == null || !m_StudentAnswers.TryGetValue(CurrentQuestion.Id, out var studentAnswers)) return 0;

                return studentAnswers.Count(student => !string.IsNullOrEmpty(student.Answer));
            }
        }

        [JsonIgnore]
        public Dictionary<string /* Choice */, int> AnswerSummaryDic
        {
            get
            {
                var answerSummary = new Dictionary<string, int>();

                if(!m_StudentAnswers.TryGetValue(CurrentQuestion.Id, out var studentAnswers)) return answerSummary;

                foreach(var choice in studentAnswers.Select(studentAnswer => studentAnswer.Answer))
                {
                    if(answerSummary.TryGetValue(choice, out var amount))
                    {
                        answerSummary[choice] = amount + 1;
                    }
                    else
                    {
                        answerSummary.Add(choice, 1);
                    }
                }

                return answerSummary;
            }
        }

        public void SetQuizList(List<Question> quizList)
        {
            m_QuizList.Clear();
            m_StudentAnswers.Clear();

            foreach(var quiz in quizList)
            {
                if(m_QuizList.ContainsKey(quiz.Id))
                {
                    m_QuizList[quiz.Id] = new Question(quiz.Id, quiz.AssetFile, quiz.Answer, quiz.QuestionAssetType);
                }
                else
                {
                    m_QuizList.Add(quiz.Id, new Question(quiz.Id, quiz.AssetFile, quiz.Answer, quiz.QuestionAssetType));
                }
            }
        }

        public void SetQuizInfo(QuizInfo quizInfo)
        {
            QuizInfo = quizInfo;
            OnQuizInfoChange?.Invoke(quizInfo);
        }

        public void SetCurrentQuestionById(string id)
        {
            if(m_QuizList.TryGetValue(id, out var quiz))
            {
                CurrentQuestion = quiz;
                OnQuestionChange?.Invoke(quiz);
            }
            else
            {
                Debug.LogError($"Quiz id: {id} not exists");
            }
        }

        public void SetCurrentQuestion(Question question)
        {
            CurrentQuestion = question;
            OnQuestionChange?.Invoke(question);
        }

        public void SetCorrectAnswer(string answer)
        {
            CorrectAnswer = answer;
            OnAnswerChange?.Invoke(answer);
        }

        public void AddStudentAnswer(string stationId)
        {
            var currentQuestionId = CurrentQuestion.Id;

            if(currentQuestionId == null) return;

            if(m_StudentAnswers.TryGetValue(currentQuestionId, out var answers))
            {
                var studentAnswer = answers.Find(info => info.StationId == stationId);

                if(studentAnswer == null) answers.Add(new StudentAnswer { StationId = stationId, Answer = "" });
            }
            else
            {
                m_StudentAnswers.Add(currentQuestionId, new List<StudentAnswer>
                {
                    new StudentAnswer { StationId = stationId, Answer = "" }
                });
            }
        }

        public void SetStudentAnswer(string stationId, string answer)
        {
            var currentQuestionId = CurrentQuestion.Id;

            if(m_StudentAnswers.TryGetValue(currentQuestionId, out var answers))
            {
                var studentAnswer = answers.Find(info => info.StationId == stationId);

                if(studentAnswer == null)
                {
                    answers.Add(new StudentAnswer { StationId = stationId, Answer = answer });
                }
                else
                {
                    studentAnswer.Answer = answer;
                }
            }
            else
            {
                m_StudentAnswers.Add(currentQuestionId,
                    new List<StudentAnswer>
                    {
                        new StudentAnswer { StationId = stationId, Answer = answer }
                    }
                );
            }

            OnStudentAnswerChange?.Invoke();
        }

        public void SetStudentPreTestResult(Dictionary<string, int> studentPreTestResult)
        {
            m_StudentPreTestResult = studentPreTestResult;
        }

        public void SetCurrentQuizSequenceState(int quizState, bool isFollowing)
        {
            CurrentQuizState = quizState;
            OnQuizStateChange?.Invoke(quizState, isFollowing);
        }

        public void SetQuizResult(int correctAnswerAmount)
        {
            CorrectAnswerAmount = correctAnswerAmount;

            OnCorrectAnswerAmountChange?.Invoke(correctAnswerAmount);
        }

        public void SetPreTestResult(PreTestResult preTestResult)
        {
            PreTestResult = preTestResult;

            OnPreTestResultChange?.Invoke(preTestResult);
        }

        public void SetCustomData(byte[] data)
        {
            CustomData = data;

            OnCustomDataChange?.Invoke(data);
        }
    }

    public readonly struct QuizInfo
    {
        public readonly int CurrentQuizNumber;

        public readonly int QuestionAmount;

        public QuizInfo(int currentQuizNumber, int questionAmount)
        {
            CurrentQuizNumber = currentQuizNumber;
            QuestionAmount = questionAmount;
        }
    }

    public class StudentAnswer
    {
        public string StationId;

        public string Answer;
    }

    public class PreTestResult
    {
        public bool HasScore;

        public int Score;

        public int FullScore;

        public PreTestResult(bool hasScore, int score, int fullScore)
        {
            HasScore = hasScore;
            Score = score;
            FullScore = fullScore;
        }
    }

    public enum QuestionAssetType
    {
        [EnumMember(Value = "IMAGE")]
        IMAGE,

        [EnumMember(Value = "VIDEO")]
        VIDEO
    }
}