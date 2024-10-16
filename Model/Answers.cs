namespace Exam.Model
{
    public class Answers
    {
        public string Answer { get; set; }
        public int Corect { get; set; }
    }

    public class Questions
    {
        public int id { get; set; }
        public string Question { get; set; }
        public List<Answers> Answers { get; set; }
    }
}
