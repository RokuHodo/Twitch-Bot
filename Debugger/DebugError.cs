namespace TwitchBot.Debugger
{
    static class DebugError
    {
        public static string NORMAL_NULL = "cannot be null",
                             NORMAL_UNKNOWN = "unknown error",
                             NORMAL_EXCEPTION = "compiler exception",
                             NORMAL_EXISTS_NO = "does not exist",
                             NORMAL_EXISTS_YES = "already exists",
                             NORMAL_SYNTAX = "bad syntax",
                             NORMAL_PERMANENT = "is permanent",
                             NORMAL_CONVERT = "could not convert object",
                             NORMAL_OUT_OF_BOUNDS = "index out of bounds",
                             NORMAL_SERIALIZE = "failed to serialize";

        public static string SYNTAX_NULL = "bad syntax, cannot be null",
                             SYNTAX_UNKNOWN = "bad syntax, unknown error",
                             SYNTAX_SQUARE_BRACKETS_NO = "bad syntax, cannot contain square brackets",
                             SYNTAX_SQUARE_BRACKETS_ENCLOSED_YES = "must be enclosed in square brackets",
                             SYNTAX_BRACKETS_NO = "bad syntax, cannot contain brackets",
                             SYNTAX_BRACKETS_ENCLOSED_YES = "bad syntax, must be enclosed in brackets",
                             SYNTAX_EQUAL_SIGN_NO = "bad syntax, cannot contain equal signs",
                             SYNTAX_SPACES_NO = "bad syntax, cannot contain spaces",
                             SYNTAX_EXCLAMATION_POINT_LEAD_YES = "bad syntax, must be lead by an exclamation point",
                             SYNTAX_LENGTH = "bad syntax, incorrect length",
                             SYNTAX_PARENTHESIS_NO = "bad syntax, cannot contain parenthesis",
                             SYNTAX_PARENTHESIS_YES = "bad syntax, must contain parenthesis",
                             SYNTAX_OUR_OF_BOUNDS = "bad syntax, index out of bounds",
                             SYNTAX_POSITIVE_YES = "bad syntax, value must be positive or zero";
    }
}
