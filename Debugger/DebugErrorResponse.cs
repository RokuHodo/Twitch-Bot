using TwitchChatBot.Enums.Debugger;

namespace TwitchChatBot.Debugger
{
    class DebugErrorResponse
    {
        /// <summary>
        /// Gets the error message to append to the failed operation message
        /// </summary>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        /// <returns></returns>
        public string GetError(DebugError error)
        {            
            string reason = "";

            switch (error)
            {
                case DebugError.ExistNo:
                    reason = "does not exists";
                    break;
                case DebugError.ExistYes:
                    reason = "already exists";
                    break;
                case DebugError.Syntax:
                    reason = "incorrect syntax";
                    break;
                case DebugError.Exception:
                    reason = "compiler exception";
                    break;
                case DebugError.Permanent:
                    reason = "is permanent";
                    break;
                case DebugError.Null:
                    reason = "value cannot be null";
                    break;
                default:
                    reason = "unknown error"; ;
                    break;
            }

            return reason;
        }

        /// <summary>
        /// Gets the error message to append to the failed syntax operation message
        /// </summary>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        /// <returns></returns>
        public string GetError(SyntaxError error)
        {
            string reason = "";

            switch (error)
            {
                case SyntaxError.SquareBracketsNo:
                    reason = "cannot contain square brackets";
                    break;                    
                case SyntaxError.SquareBracketsYes:
                    reason = "must be enclosed in square brackets";
                    break;
                case SyntaxError.BracketsNo:
                    reason = "cannot contain brackets";
                    break;
                case SyntaxError.BracketsYes:
                    reason = "must be enclosed in brackets";
                    break;
                case SyntaxError.EqualSigns:
                    reason = "cannot contain equal signs";
                    break;
                case SyntaxError.Spaces:
                    reason = "cannot contain spaces";
                    break;
                case SyntaxError.Null:
                    reason = "cannot be null";
                    break;
                case SyntaxError.EexclamationPoint:
                    reason = "must be lead by an exclamation point";
                    break;
                case SyntaxError.NullArray:
                    reason = "array cannot be null";
                    break;
                case SyntaxError.Length:
                    reason = "- incorrect string length";
                    break;
                case SyntaxError.ArrayLength:
                    reason = "- incorrect array length";
                    break;
                default:
                    reason = "unknown error"; ;
                    break;
            }

            return reason;
        }
    }
}
