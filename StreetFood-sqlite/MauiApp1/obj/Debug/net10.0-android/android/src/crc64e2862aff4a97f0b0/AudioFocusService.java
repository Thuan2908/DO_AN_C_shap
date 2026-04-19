package crc64e2862aff4a97f0b0;


public class AudioFocusService
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.media.AudioManager.OnAudioFocusChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAudioFocusChange:(I)V:GetOnAudioFocusChange_IHandler:Android.Media.AudioManager+IOnAudioFocusChangeListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("MauiApp1.Platforms.Android.AudioFocusService, MauiApp1", AudioFocusService.class, __md_methods);
	}

	public AudioFocusService ()
	{
		super ();
		if (getClass () == AudioFocusService.class) {
			mono.android.TypeManager.Activate ("MauiApp1.Platforms.Android.AudioFocusService, MauiApp1", "", this, new java.lang.Object[] {  });
		}
	}

	public void onAudioFocusChange (int p0)
	{
		n_onAudioFocusChange (p0);
	}

	private native void n_onAudioFocusChange (int p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
