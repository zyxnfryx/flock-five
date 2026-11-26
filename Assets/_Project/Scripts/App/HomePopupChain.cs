namespace FlockFive
{
    // After poker (and any other home return): one card at a time.
    // Reminder, then the Welcome bonus, then the ATT sheet.
    public static class HomePopupChain
    {
        public enum Card
        {
            None = 0,
            Reminder = 1,
            Welcome = 2,
            Att = 3,
        }

        public static Card Next(bool reminderDue, bool reminderOpen, bool welcomePending, bool welcomeOpen, bool attDue)
        {
            if (reminderOpen || reminderDue) return Card.Reminder;
            if (welcomeOpen || welcomePending) return Card.Welcome;
            if (attDue) return Card.Att;
            return Card.None;
        }
    }
}
