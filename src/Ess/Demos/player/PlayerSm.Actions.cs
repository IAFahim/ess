namespace Machines
{
    public partial struct PlayerSm
    {
        void OnPlay()
        {
            vars.playCount += 1;
        }

        void OnPause()
        {
            vars.pauseCount += 1;
        }

        void OnLeave()
        {
            vars.leaveCount += 1;
        }
    }
}
