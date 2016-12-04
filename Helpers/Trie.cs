using System.Collections.Generic;

namespace TwitchBot.Helpers
{
    class Trie
    {
        Node root;

        public Trie()
        {
            root = new Node();
        }

        /// <summary>
        /// Adds a word into the try and returns if it is successfull.
        /// </summary>
        public bool Insert(string word)
        {
            bool added = false;
              
            word = word.ToLower();

            //always start at the root node
            Node current_node = root;

            for (int index = 0; index < word.Length; ++index)
            {
                char letter = word[index];

                Node child;

                //check to see if the letter already exists in the children nodes
                if(!current_node.children.TryGetValue(letter, out child))
                {
                    //the letter did not exist in the current nodes, add the letter
                    child = new Node();                    
                    current_node.children.Add(letter, child);
                }

                //update the node and iterate
                current_node = child;
            }

            if (!current_node.complete_word)
            {
                current_node.complete_word = true;

                added = true;
            }

            return added;            
        }

        /// <summary>
        /// Checks to see if a word is already in the trie.
        /// </summary>   
        public bool Match(string word)
        {
            word = word.ToLower();

            Node current_node = root;

            for (int index = 0; index < word.Length; index++)
            {
                char letter = word[index];

                Node child;
                
                if (!current_node.children.TryGetValue(letter, out child))
                {
                    return false;
                }

                current_node = child;
            }

            return current_node.complete_word;
        }

        /// <summary>
        /// Removes a word from the trie.
        /// </summary>
        public void Delete(string word)
        {
            Delete(root, word, 0);
        }

        /// <summary>
        /// Recursive function that deletes a word from the trie one character at a time starting from the root.
        /// </summary>
        private bool Delete(Node current_node, string word, int index)
        {
            //only applicable for the node directly after the last letter in the word
            if (index == word.Length)
            {
                //the word was never instered into the trie, don't do anything
                if (!current_node.complete_word)
                {
                    return false;
                }

                //set the node to false to "delete" the word
                current_node.complete_word = false;

                //delete the current node if it has no children
                return current_node.children.Count == 0;
            }

            char letter = word[index];

            Node child;

            if (!current_node.children.TryGetValue(letter, out child))
            {
                //no node has the character associated with it, do nothing
                return false;
            }

            //the child node can safely be deleted
            if (Delete(child, word, index + 1))
            {                
                current_node.children.Remove(letter);

                //delete the current node if it has no children
                return current_node.children.Count == 0;
            }

            return false;
        }

        #region Node

        private class Node
        {
            public bool complete_word;

            public Dictionary<char, Node> children;

            public Node()
            {
                complete_word = false;

                children = new Dictionary<char, Node>();
            }
        }

        #endregion

    }
}
