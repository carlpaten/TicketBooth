CREATE TABLE Orders
(
	OrderId             INTEGER PRIMARY KEY,
	PurchaseTime        DATETIME,
	ThunderTixOrderId   INT NOT NULL UNIQUE,
	FirstName           VARCHAR(50),
	LastName            VARCHAR(50),
	Email               VARCHAR(50),
	Price               INTEGER, -- in cents
	Payment             VARCHAR(50),
	Comments            VARCHAR(140),
	StaffComments       VARCHAR(140)
);

CREATE TABLE Tickets
(
	TicketId    INTEGER PRIMARY KEY,
	OrderId     INTEGER,
	Barcode     VARCHAR(10),
	TicketType  VARCHAR(30),
	FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);