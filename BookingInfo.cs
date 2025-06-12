public struct BookingInfo {
	public int bookingId;
	public int kundenId;
	public DateTime bookingDate;
	public DateTime bookingStart;
	public DateTime bookingEnd;

	// just store this stuff here to use later
	public BookingInfo(int bid,int kid,DateTime bdate,DateTime bstart,DateTime bend){
		bookingId = bid;
		kundenId = kid;
		bookingDate = bdate;
		bookingStart = bstart;
		bookingEnd = bend;
	}
}
