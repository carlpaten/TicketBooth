CREATE TABLE Orders
(
	OrderId             INT,
	PurchaseTime        DATETIME,
	ThunderTixOrderId   INT NOT NULL UNIQUE,
	FirstName           VARCHAR(50),
	LastName            VARCHAR(50),
	Email               VARCHAR(50),
	Price               INT, -- in cents
	Payment             VARCHAR(50),
	Comments            VARCHAR(140),
	StaffComments       VARCHAR(140),
	PRIMARY KEY (OrderId)
);

CREATE TABLE Tickets
(
	TicketId    INT,
	OrderId     INT,
	Barcode     VARCHAR(10),
	TicketType  VARCHAR(30),
	PRIMARY KEY (TicketId),
	FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);