using System;

namespace API.Extensions {
    public static class DateTimeExtensions {
        public static int CalculateAge(this DateTime dateOfBirth) {
            DateTime today = DateTime.Today;
            int age = today.Year - dateOfBirth.Year;

            if(dateOfBirth.Date > today.AddYears(-age))
                age--;

            return age;
        }
    }
}