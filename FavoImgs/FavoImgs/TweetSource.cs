namespace FavoImgs
{
    enum TweetSource
    {
        Invalid = 0,

        /// <summary>
        /// 임의의 사용자의 트윗
        /// </summary>
        Tweets,

        /// <summary>
        /// 임의의 사용자의 관심글
        /// </summary>
        Favorites,

        /// <summary>
        /// 나의 리스트
        /// </summary>
        Lists,
    };
}
