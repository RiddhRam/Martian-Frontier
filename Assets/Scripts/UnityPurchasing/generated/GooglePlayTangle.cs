// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("/Xw59rYy/I7W/mRuo6GLwfE++SZK59VZRQc/XDrDvopa1WxItwH/grXyxq6e6jvznXSnLbjpvzhWiAH3lhcDnO2R3qnBLK3/5qf+efJaadRPiNpzMVGvxLg/JwSOTw/26pmALx+tLg0fIikmBalnqdgiLi4uKi8sRt4Px4A3sEhi5f6DotikHz2tXGJ/YcTu76iyWJ6BitFsH1W0au0lKbkVPz15B3Q0fT2/3PYHo1l19h3w4xv8q/FN9jJTkao5pDCTlX3Lcw2tLiAvH60uJS2tLi4vl4zi5ATUwfYtWmE8ckf0pVPvG6qEMA339t/H5qDeJTrwKuxUgzXE6rbJivSKUUTb6nMFt9Eiu79U0QT4sDFUxffR8y/mKWrZUCNypi0sLi8u");
        private static int[] order = new int[] { 10,12,8,3,13,10,7,7,13,9,12,13,13,13,14 };
        private static int key = 47;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
