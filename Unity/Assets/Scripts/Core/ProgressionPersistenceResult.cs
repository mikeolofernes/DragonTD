namespace DragonTD.Core
{
    public class ProgressionPersistenceResult
    {
        public bool success;
        public string message;
        public PlayerProgressionSaveData saveData;

        public static ProgressionPersistenceResult Success(string message, PlayerProgressionSaveData saveData = null)
        {
            return new ProgressionPersistenceResult { success = true, message = message, saveData = saveData };
        }

        public static ProgressionPersistenceResult Failure(string message)
        {
            return new ProgressionPersistenceResult { success = false, message = message };
        }
    }
}
